namespace game.api.Models
{
    public class GameScore
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime DateAchieved { get; set; }
        public string? LinkedInProfileUrl { get; set; }
        public Game Game { get; set; } = null!;
    }
}