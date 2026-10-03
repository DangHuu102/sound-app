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
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public static class DatabaseManager
    {
        private static string GetDbPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "soundapp.db");
        }

        private static string GetConnectionString()
        {
            return $"Data Source={GetDbPath()}";
        }

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS PlayHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Url TEXT NOT NULL,
                        ThumbnailUrl TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Email TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        DisplayName TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );");
            }
        }

        // Hash mật khẩu đơn giản (production nên dùng BCrypt)
        private static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "soundstudio_salt"));
            return Convert.ToHexString(bytes);
        }

        public static bool Register(string email, string password, string displayName)
        {
            try
            {
                using var connection = new SqliteConnection(GetConnectionString());
                connection.Execute(
                    "INSERT INTO Users (Email, PasswordHash, DisplayName) VALUES (@Email, @Hash, @Name)",
                    new { Email = email.ToLower(), Hash = HashPassword(password), Name = displayName });
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
                connection.Execute("DELETE FROM PlayHistory WHERE Url = @Url", new { Url = url });
                
                var insertCmd = @"
                    INSERT INTO PlayHistory (Title, Url, ThumbnailUrl, CreatedAt)
                    VALUES (@Title, @Url, @ThumbnailUrl, @CreatedAt);
                ";
                connection.Execute(insertCmd, new { Title = title, Url = url, ThumbnailUrl = thumbnailUrl, CreatedAt = DateTime.Now });
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

        public static void CreatePlaylist(string name)
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Execute("INSERT INTO Playlists (Name) VALUES (@Name)", new { Name = name });
            }
        }
    }
}
