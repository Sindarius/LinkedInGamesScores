namespace game.api.Models
{
    public enum ScoringType
    {
        Guesses = 1,
        Time = 2
    }

    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public ScoringType ScoringType { get; set; } = ScoringType.Guesses;
        public List<GameScore> Scores { get; set; } = new List<GameScore>();
    }
}