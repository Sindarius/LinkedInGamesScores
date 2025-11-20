namespace game.api.Models
{
    /// <summary>
    /// Separate table for storing game score images to improve query performance.
    /// This prevents loading large byte arrays when querying game scores.
    /// </summary>
    public class GameScoreImage
    {
        public int Id { get; set; }
        public int GameScoreId { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "image/jpeg";
        public DateTime UploadedDate { get; set; }

        // Navigation property
        public GameScore? GameScore { get; set; }
    }
}
