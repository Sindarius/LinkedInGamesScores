namespace game.api.Models
{
    public class DailyChampionsDto
    {
        public int GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public ScoringType ScoringType { get; set; }
        public List<GameScoreDto> Champions { get; set; } = new();
        public int? Score { get; set; }
        public int? GuessCount { get; set; }
        public TimeSpan? CompletionTime { get; set; }
        public DateTime? DateAchieved { get; set; }
    }
}

