namespace game.api.Models
{
    public class GameDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public ScoringType ScoringType { get; set; } = ScoringType.Guesses;
    }
}