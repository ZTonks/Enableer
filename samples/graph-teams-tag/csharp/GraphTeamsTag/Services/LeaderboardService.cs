using System.Text.Json;
using GraphTeamsTag.Models;

namespace GraphTeamsTag.Services
{
    public class LeaderboardService
    {
        private const string FilePath = "leaderboard.json";
        private readonly object _lock = new object();

        public List<LeaderboardEntry> GetLeaderboard()
        {
            lock (_lock)
            {
                if (!File.Exists(FilePath)) return new List<LeaderboardEntry>();
                var json = File.ReadAllText(FilePath);
                try
                {
                    return JsonSerializer.Deserialize<List<LeaderboardEntry>>(json) ?? new List<LeaderboardEntry>();
                }
                catch
                {
                    return new List<LeaderboardEntry>();
                }
            }
        }

        public void AddPoints(string userId, string displayName, int points)
        {
            lock (_lock)
            {
                var leaderboard = GetLeaderboard();
                var entry = leaderboard.FirstOrDefault(e => e.UserId == userId);
                if (entry == null)
                {
                    entry = new LeaderboardEntry { UserId = userId, DisplayName = displayName, Points = 0 };
                    leaderboard.Add(entry);
                }
                entry.Points += points;
                
                if (!string.IsNullOrEmpty(displayName))
                {
                    entry.DisplayName = displayName;
                }

                File.WriteAllText(FilePath, JsonSerializer.Serialize(leaderboard, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }
}

