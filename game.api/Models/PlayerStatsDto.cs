namespace game.api.Models
{
    public class PlayerStreakDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public string? LinkedInProfileUrl { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
    }

    public class RankHistoryEntryDto
    {
        public string Date { get; set; } = string.Empty;
        public int? Rank { get; set; }
        public double? Score { get; set; }
    }

    public class RecentResultDto
    {
        public int ScoreId { get; set; }
        public int GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public ScoringType ScoringType { get; set; }
        public string Date { get; set; } = string.Empty;
        public int? GuessCount { get; set; }
        public TimeSpan? CompletionTime { get; set; }
        public double Score { get; set; }
        public int Rank { get; set; }
        public bool IsDnf { get; set; }
        public bool HasImage { get; set; }
    }

    public class PlayerGameStatsDto
    {
        public int GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public ScoringType ScoringType { get; set; }
        public int TotalGames { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double AvgRank { get; set; }
        public double? AvgScore { get; set; }
        public double? BestScore { get; set; }
        public bool NeverDnf { get; set; }
        public List<RankHistoryEntryDto> RankHistory { get; set; } = new();
    }

    public class PlayerSummaryDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public string? LinkedInProfileUrl { get; set; }
        public int TotalGames { get; set; }
        public int Wins { get; set; }
        public int CurrentStreak { get; set; }
        public List<string> GameNames { get; set; } = new();
    }

    public class PlayerStatsDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public string? LinkedInProfileUrl { get; set; }
        public int TotalGames { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public List<string> Achievements { get; set; } = new();
        public List<PlayerGameStatsDto> GameStats { get; set; } = new();
        public List<RecentResultDto> RecentResults { get; set; } = new();
    }
}
