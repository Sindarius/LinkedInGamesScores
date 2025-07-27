namespace game.api.Models
{
    public class GameScoreWithImageDto
    {
        public int GameId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? GuessCount { get; set; }
        public TimeSpan? CompletionTime { get; set; }
        public string? LinkedInProfileUrl { get; set; }
        public IFormFile? ScoreImage { get; set; }
    }
}