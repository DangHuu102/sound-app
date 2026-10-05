using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace soundapp
{
    public class TrackItem
    {
        public string Title { get; set; } = "";
        public string ArtistName { get; set; } = "";
        public string YoutubeUrl { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
    }

    public static class RecommendationService
    {
        private static readonly YoutubeClient _youtube = new YoutubeClient();

        public static async Task<List<TrackItem>> GetRecommendationsAsync(string currentTitle, string currentArtist)
        {
            var results = new List<TrackItem>();
            try
            {
                // Nhận diện các thể loại đặc thù (Remix, Lofi, Vinahouse, Nonstop, Mashup)
                string lowerTitle = currentTitle.ToLower();
                string specialVibe = "";
                if (lowerTitle.Contains("remix")) specialVibe = "remix";
                else if (lowerTitle.Contains("lofi")) specialVibe = "lofi";
                else if (lowerTitle.Contains("vinahouse")) specialVibe = "vinahouse";
                else if (lowerTitle.Contains("nonstop")) specialVibe = "nonstop";
                else if (lowerTitle.Contains("mashup")) specialVibe = "mashup";

                // Xây dựng query thông minh
                string searchQuery = "";
                if (string.IsNullOrWhiteSpace(currentArtist) || currentArtist == "Unknown Artist")
                {
                    searchQuery = $"{currentTitle} {specialVibe} audio";
                }
                else
                {
                    // Nếu là nhạc đặc thù thì tìm "Ca sĩ + Thể loại", nếu không thì tìm "Ca sĩ + bài hát tương tự"
                    searchQuery = string.IsNullOrEmpty(specialVibe) 
                        ? $"{currentArtist} official audio playlist" 
                        : $"{currentArtist} {specialVibe} track";
                }

                // Sử dụng YoutubeExplode Search để lấy các bài liên quan
                var searchResults = await _youtube.Search.GetVideosAsync(searchQuery).CollectAsync(15);
                
                foreach (var video in searchResults)
                {
                    // Bỏ qua video quá dài (podcast/livestream)
                    if (video.Duration.HasValue && video.Duration.Value.TotalMinutes > 10) continue;
                    
                    var meta = DatabaseManager.ParseMetadata(video.Title);
                    
                    // Bỏ qua nếu là chính bài đang nghe
                    if (meta.SongTitle.Equals(currentTitle, StringComparison.OrdinalIgnoreCase)) continue;

                    results.Add(new TrackItem
                    {
                        Title = meta.SongTitle,
                        ArtistName = meta.Artist,
                        YoutubeUrl = video.Url,
                        ThumbnailUrl = video.Thumbnails.GetWithHighestResolution().Url
                    });

                    if (results.Count >= 10) break; // Lấy 10 bài
                }
            }
            catch { }
            return results;
        }
    }
}
