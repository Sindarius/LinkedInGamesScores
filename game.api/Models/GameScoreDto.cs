namespace game.api.Models
{
    public class GameScoreDto
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? GuessCount { get; set; }
        public TimeSpan? CompletionTime { get; set; }
        public int Score { get; set; }
        public DateTime DateAchieved { get; set; }
        public string? LinkedInProfileUrl { get; set; }
        public string? GameName { get; set; }
        public ScoringType ScoringType { get; set; }
    }
}