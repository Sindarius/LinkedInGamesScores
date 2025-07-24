namespace game.api.Models
{
    public class GameScore
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? GuessCount { get; set; }
        public TimeSpan? CompletionTime { get; set; }
        public DateTime DateAchieved { get; set; }
        public string? LinkedInProfileUrl { get; set; }
        public Game? Game { get; set; }

        public int Score => Game?.ScoringType == ScoringType.Time 
            ? (int)(CompletionTime?.TotalSeconds ?? 0)
            : (GuessCount ?? 0);
    }
}