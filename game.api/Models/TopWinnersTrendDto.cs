namespace game.api.Models
{
    public class TopWinnersSeriesDto
    {
        public string PlayerId { get; set; } = string.Empty; // LinkedIn URL or normalized name
        public string PlayerName { get; set; } = string.Empty;
        public string? ProfileUrl { get; set; }
        public int[] Data { get; set; } = Array.Empty<int>(); // wins per day
        public int Total { get; set; }
    }

    public class TopWinnersTrendDto
    {
        public List<string> Labels { get; set; } = new(); // yyyy-MM-dd per day
        public List<TopWinnersSeriesDto> Series { get; set; } = new();
        public int Days { get; set; }
    }
}

