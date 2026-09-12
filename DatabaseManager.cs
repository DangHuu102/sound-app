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
                var createTableCmd = @"
                    CREATE TABLE IF NOT EXISTS PlayHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Url TEXT NOT NULL,
                        ThumbnailUrl TEXT NOT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );
                ";
                connection.Execute(createTableCmd);
            }
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
    }
}
