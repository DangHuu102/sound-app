using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;

namespace soundapp
{
    public class PlayHistory
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string DisplayTitle => Title.Replace("\n", " - ");
        public string Url { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string University { get; set; } = "";
        public string StudentId { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class StreamCacheItem
    {
        public string StreamUrl { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }

    public static class DatabaseManager
    {
        private static string GetDbPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "soundapp.db");
        }

        private static string GetConnectionString()
        {
            return $"Data Source={GetDbPath()};Default Timeout=5;";
        }

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                connection.Execute("PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;");
                
                // Cố gắng thêm cột nếu bảng đã tồn tại (dùng try-catch vì SQLite không có ADD COLUMN IF NOT EXISTS)
                try { connection.Execute("ALTER TABLE Users ADD COLUMN University TEXT;"); } catch { }
                try { connection.Execute("ALTER TABLE Users ADD COLUMN StudentId TEXT;"); } catch { }

                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Email TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        DisplayName TEXT NOT NULL,
                        University TEXT,
                        StudentId TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE TABLE IF NOT EXISTS Tracks (
                        Id TEXT PRIMARY KEY,
                        Title TEXT NOT NULL,
                        ArtistName TEXT,
                        YoutubeUrl TEXT UNIQUE,
                        ThumbnailUrl TEXT
                    );
                    CREATE TABLE IF NOT EXISTS StreamCache (
                        YoutubeUrl TEXT PRIMARY KEY,
                        StreamUrl TEXT NOT NULL,
                        ExpiresAt DATETIME NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS PlayHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Url TEXT NOT NULL,
                        ThumbnailUrl TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );");

                // Seed tài khoản Admin mặc định (nếu chưa tồn tại)
                var adminExists = connection.QueryFirstOrDefault<int>("SELECT COUNT(1) FROM Users WHERE Email = 'admin@soundstudio.com'");
                if (adminExists == 0)
                {
                    connection.Execute(
                        "INSERT INTO Users (Email, PasswordHash, DisplayName, University, StudentId) VALUES (@Email, @Hash, @Name, @Uni, @StudentId)",
                        new { Email = "admin@soundstudio.com", Hash = HashPassword("admin"), Name = "admin", Uni = "System", StudentId = "000000" });
                }

                // Xóa toàn bộ StreamCache để loại bỏ các URL định dạng webm bị lỗi lưu từ bản cũ
                connection.Execute("DELETE FROM StreamCache");
            }
        }

        public static (string Artist, string SongTitle) ParseMetadata(string youtubeTitle)
        {
            // 1. Loại bỏ các noise tags: (Official Music Video), [MV], (Lyric Video), 4K...
            string cleaned = System.Text.RegularExpressions.Regex.Replace(
                youtubeTitle, 
                @"\s*[\[\(](official.*?|audio|mv|lyrics?|visualizer|live|4k|hd)[\)\]]", 
                "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned, 
                @"\s*(?://|\||-)\s*(?:OFFICIAL.*?|MV|LYRIC.*?)$", 
                "", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 2. Tách theo dấu " - ", " – ", " | "
            var parts = cleaned.Split(new[] { " - ", " – ", " | " }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }
            return ("Unknown Artist", cleaned.Trim());
        }

        public static string? GetValidStreamUrl(string youtubeUrl)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                var cache = connection.QueryFirstOrDefault<StreamCacheItem>("SELECT StreamUrl, ExpiresAt FROM StreamCache WHERE YoutubeUrl = @Url", new { Url = youtubeUrl });
                if (cache != null && cache.ExpiresAt > DateTime.UtcNow)
                {
                    return cache.StreamUrl;
                }
                return null;
            }
        }

        public static void SaveStreamUrl(string youtubeUrl, string streamUrl)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                var expiresAt = DateTime.UtcNow.AddHours(4); // Hạn của Youtube thường là 6h, đặt 4h cho an toàn
                connection.Execute(@"
                    INSERT OR REPLACE INTO StreamCache (YoutubeUrl, StreamUrl, ExpiresAt) 
                    VALUES (@YoutubeUrl, @StreamUrl, @ExpiresAt)", 
                    new { YoutubeUrl = youtubeUrl, StreamUrl = streamUrl, ExpiresAt = expiresAt });
            }
        }

        // Hash mật khẩu đơn giản (production nên dùng BCrypt)
        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "soundstudio_salt"));
            return Convert.ToHexString(bytes);
        }

        public static bool Register(string email, string password, string displayName, string university, string studentId)
        {
            try
            {
                using var connection = new SqliteConnection(GetConnectionString());
                connection.Execute(
                    "INSERT INTO Users (Email, PasswordHash, DisplayName, University, StudentId) VALUES (@Email, @Hash, @Name, @Uni, @StudentId)",
                    new { Email = email.ToLower(), Hash = HashPassword(password), Name = displayName, Uni = university, StudentId = studentId });
                return true;
            }
            catch { return false; } // Email đã tồn tại
        }

        public static User? Login(string emailOrUsername, string password)
        {
            using var connection = new SqliteConnection(GetConnectionString());
            // Cho phép đăng nhập bằng email HOẶC username (DisplayName)
            return connection.QueryFirstOrDefault<User>(
                @"SELECT * FROM Users 
                  WHERE (LOWER(Email) = LOWER(@Input) OR LOWER(DisplayName) = LOWER(@Input))
                  AND PasswordHash = @Hash",
                new { Input = emailOrUsername.Trim(), Hash = HashPassword(password) });
        }

        public static void AddHistory(string title, string url, string thumbnailUrl)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                using var tran = connection.BeginTransaction();
                try
                {
                    var meta = ParseMetadata(title);
                    
                    connection.Execute(@"
                        INSERT OR IGNORE INTO Tracks (Id, Title, ArtistName, YoutubeUrl, ThumbnailUrl)
                        VALUES (@Id, @Title, @ArtistName, @YoutubeUrl, @ThumbnailUrl)",
                        new { Id = Guid.NewGuid().ToString(), Title = meta.SongTitle, ArtistName = meta.Artist, YoutubeUrl = url, ThumbnailUrl = thumbnailUrl }, tran);

                    connection.Execute("DELETE FROM PlayHistory WHERE Url = @Url", new { Url = url }, tran);
                    
                    connection.Execute(@"
                        INSERT INTO PlayHistory (Title, Url, ThumbnailUrl, CreatedAt)
                        VALUES (@Title, @Url, @ThumbnailUrl, @CreatedAt)",
                        new { Title = meta.SongTitle + "\n" + meta.Artist, Url = url, ThumbnailUrl = thumbnailUrl, CreatedAt = DateTime.Now }, tran);
                        
                    tran.Commit();
                }
                catch { tran.Rollback(); }
            }
        }

        public static List<PlayHistory> GetHistory()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                return connection.Query<PlayHistory>("SELECT * FROM PlayHistory ORDER BY CreatedAt DESC LIMIT 50").ToList();
            }
        }

        public static void ClearHistory()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Execute("DELETE FROM PlayHistory");
            }
        }

        public static void InitializePlaylists()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS Playlists (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE TABLE IF NOT EXISTS PlaylistItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        PlaylistId INTEGER,
                        Title TEXT,
                        Url TEXT,
                        ThumbnailUrl TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(PlaylistId) REFERENCES Playlists(Id)
                    );");
            }
        }

        public static List<Playlist> GetPlaylists()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                return connection.Query<Playlist>("SELECT * FROM Playlists ORDER BY CreatedAt DESC").ToList();
            }
        }

        public static List<PlayHistory> GetPlaylistItems(int playlistId)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                return connection.Query<PlayHistory>(
                    "SELECT Id, Title, Url, ThumbnailUrl, CreatedAt FROM PlaylistItems WHERE PlaylistId = @PlaylistId ORDER BY CreatedAt ASC",
                    new { PlaylistId = playlistId }).ToList();
            }
        }

        public static void CreatePlaylist(string name)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Execute("INSERT INTO Playlists (Name) VALUES (@Name)", new { Name = name });
            }
        }

        public static void AddTrackToPlaylist(int playlistId, string title, string url, string thumbUrl)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                // Tránh thêm trùng bài hát vào cùng 1 playlist
                var exists = connection.QueryFirstOrDefault<int>(
                    "SELECT COUNT(1) FROM PlaylistItems WHERE PlaylistId = @PlaylistId AND Url = @Url",
                    new { PlaylistId = playlistId, Url = url });

                if (exists == 0)
                {
                    connection.Execute(
                        "INSERT INTO PlaylistItems (PlaylistId, Title, Url, ThumbnailUrl) VALUES (@PlaylistId, @Title, @Url, @ThumbnailUrl)",
                        new { PlaylistId = playlistId, Title = title, Url = url, ThumbnailUrl = thumbUrl });
                }
            }
        }

        public static string? SearchLocalTrack(string query)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                // Ưu tiên tìm trong bảng Tracks (dựa trên Title hoặc ArtistName)
                // Lấy kết quả có nhiều lượt nghe nhất trong PlayHistory
                var sql = @"
                    SELECT t.YoutubeUrl 
                    FROM Tracks t
                    WHERE t.Title LIKE @Query OR t.ArtistName LIKE @Query
                    ORDER BY (SELECT COUNT(*) FROM PlayHistory h WHERE h.Url = t.YoutubeUrl) DESC
                    LIMIT 1";
                
                return connection.QueryFirstOrDefault<string>(sql, new { Query = $"%{query}%" });
            }
        }
    }
}
