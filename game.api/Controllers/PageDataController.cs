using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using game.api.Data;
using game.api.Models;
using game.api.Utils;

namespace game.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PageDataController : ControllerBase
    {
        private readonly GameContext _context;

        public PageDataController(GameContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all data needed for the daily leaderboard page in one request
        /// Reduces 100+ requests to 1 request
        /// </summary>
        [HttpGet("daily")]
        public async Task<ActionResult<DailyPageDataDto>> GetDailyPageData(
            [FromQuery] DateTime? date = null,
            [FromQuery] int leaderboardTop = 10,
            [FromQuery] int statsTopWinners = 5,
            [FromQuery] int analyticsDays = 7)
        {
            try
            {
                // Use Pacific day boundaries for the selected date
                var (start, end, pacificDate) = TimeZoneHelper.GetPacificDayRange(date);

                // Get all active games
                var games = await _context.Games
                    .AsNoTracking()
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.Id)
                    .ToListAsync();

                var gameIds = games.Select(g => g.Id).ToList();

                // Get leaderboards for all games for the selected date in ONE query
                var dailyScores = await _context.GameScores
                    .AsNoTracking()
                    .Include(gs => gs.Game)
                    .Where(gs => gameIds.Contains(gs.GameId) &&
                                 gs.DateAchieved >= start &&
                                 gs.DateAchieved < end)
                    .ToListAsync();

                // Build leaderboards per game
                var leaderboards = new Dictionary<int, List<GameScoreDto>>();
                foreach (var game in games)
                {
                    var gameScores = dailyScores
                        .Where(gs => gs.GameId == game.Id &&
                                    ((game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                                     (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue)))
                        .OrderBy(gs => game.ScoringType == ScoringType.Time
                            ? gs.CompletionTime!.Value.TotalSeconds
                            : gs.GuessCount!.Value)
                        .Take(leaderboardTop)
                        .Select(gs => new GameScoreDto
                        {
                            Id = gs.Id,
                            GameId = gs.GameId,
                            PlayerName = gs.PlayerName,
                            GuessCount = gs.GuessCount,
                            CompletionTime = gs.CompletionTime,
                            Score = game.ScoringType == ScoringType.Time
                                ? (int)gs.CompletionTime!.Value.TotalSeconds
                                : gs.GuessCount!.Value,
                            DateAchieved = gs.DateAchieved,
                            LinkedInProfileUrl = gs.LinkedInProfileUrl,
                            GameName = game.Name,
                            ScoringType = game.ScoringType,
                            HasScoreImage = gs.Image != null || gs.ScoreImage != null
                        })
                        .ToList();

                    leaderboards[game.Id] = gameScores;
                }

                // Get unique player names from today's leaderboards for temperature data
                var playerNames = dailyScores
                    .Select(gs => gs.PlayerName.Trim())
                    .Distinct()
                    .ToList();

                // Get player temperature data for all players in ONE query
                var cutoffDate = DateTime.UtcNow.AddDays(-analyticsDays);
                var recentScores = await _context.GameScores
                    .AsNoTracking()
                    .Include(gs => gs.Game)
                    .Where(gs => gs.DateAchieved >= cutoffDate &&
                                 playerNames.Contains(gs.PlayerName.Trim()))
                    .ToListAsync();

                // Build player temperature data
                var playerTemperatures = new Dictionary<string, object>();
                foreach (var playerName in playerNames)
                {
                    var normalizedName = playerName.ToLower().Trim();
                    var playerScores = recentScores
                        .Where(gs => gs.PlayerName.Trim().ToLower() == normalizedName)
                        .ToList();

                    var totalScores = playerScores.Count;
                    var gamesPlayed = playerScores.Select(gs => gs.GameId).Distinct().Count();

                    // Calculate wins (1st place finishes)
                    var wins = 0;
                    var dailyScoresGrouped = playerScores.GroupBy(gs => new { gs.GameId, Date = gs.DateAchieved.Date });

                    foreach (var dayGroup in dailyScoresGrouped)
                    {
                        var allDayScores = recentScores
                            .Where(gs => gs.GameId == dayGroup.Key.GameId &&
                                        gs.DateAchieved.Date == dayGroup.Key.Date)
                            .ToList();

                        var game = games.FirstOrDefault(g => g.Id == dayGroup.Key.GameId);
                        if (game == null) continue;

                        var playerScore = dayGroup.FirstOrDefault();
                        if (playerScore == null) continue;

                        bool isWinner = false;
                        if (game.ScoringType == ScoringType.Time && playerScore.CompletionTime.HasValue)
                        {
                            var bestTime = allDayScores
                                .Where(gs => gs.CompletionTime.HasValue)
                                .Min(gs => gs.CompletionTime!.Value);
                            isWinner = playerScore.CompletionTime.Value == bestTime;
                        }
                        else if (game.ScoringType == ScoringType.Guesses && playerScore.GuessCount.HasValue)
                        {
                            var bestGuesses = allDayScores
                                .Where(gs => gs.GuessCount.HasValue)
                                .Min(gs => gs.GuessCount!.Value);
                            isWinner = playerScore.GuessCount.Value == bestGuesses;
                        }

                        if (isWinner) wins++;
                    }

                    var winRate = totalScores > 0 ? (double)wins / totalScores : 0;
                    string temperature;

                    if (winRate >= 0.6) temperature = "🔥 On Fire";
                    else if (winRate >= 0.4) temperature = "🌡️ Hot";
                    else if (winRate >= 0.2) temperature = "😊 Warm";
                    else if (winRate >= 0.1) temperature = "❄️ Cool";
                    else temperature = "🧊 Cold";

                    playerTemperatures[playerName] = new
                    {
                        PlayerName = playerName,
                        Temperature = temperature,
                        TotalScores = totalScores,
                        Wins = wins,
                        WinRate = Math.Round(winRate * 100, 1),
                        GamesPlayed = gamesPlayed,
                        DaysAnalyzed = analyticsDays
                    };
                }

                // Get analytics data (these are already optimized with single queries)
                var analyticsTask = GetAnalyticsDataAsync(analyticsDays);
                var statsTask = GetStatsDataAsync(start, end, statsTopWinners);

                await Task.WhenAll(analyticsTask, statsTask);

                var result = new DailyPageDataDto
                {
                    Date = pacificDate.ToString("yyyy-MM-dd"),
                    Leaderboards = leaderboards,
                    PlayerTemperatures = playerTemperatures,
                    Analytics = await analyticsTask,
                    Stats = await statsTask
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error loading page data: {ex.Message}");
            }
        }

        private async Task<object> GetAnalyticsDataAsync(int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var games = await _context.Games.AsNoTracking().Where(g => g.IsActive).ToListAsync();

            // Get all scores needed for analytics in one query
            var allScores = await _context.GameScores
                .AsNoTracking()
                .Include(gs => gs.Game)
                .Where(gs => gs.DateAchieved >= cutoffDate && gs.Game!.IsActive)
                .ToListAsync();

            // Calculate close calls from the scores we already have
            var closeCallsData = new List<object>();
            foreach (var game in games)
            {
                var gameScores = allScores.Where(gs => gs.GameId == game.Id).ToList();
                var dailyGroupedScores = gameScores
                    .GroupBy(s => s.DateAchieved.Date)
                    .Where(g => g.Count() > 1);

                int closeCallCount = 0;
                foreach (var dayGroup in dailyGroupedScores)
                {
                    var sortedScores = game.ScoringType == ScoringType.Time
                        ? dayGroup.OrderBy(s => s.CompletionTime!.Value.TotalSeconds).ToList()
                        : dayGroup.OrderBy(s => s.GuessCount!.Value).ToList();

                    for (int i = 0; i < sortedScores.Count - 1; i++)
                    {
                        var margin = game.ScoringType == ScoringType.Time
                            ? sortedScores[i + 1].CompletionTime!.Value.TotalSeconds - sortedScores[i].CompletionTime!.Value.TotalSeconds
                            : sortedScores[i + 1].GuessCount!.Value - sortedScores[i].GuessCount!.Value;

                        var isCloseCall = game.ScoringType == ScoringType.Time ? margin <= 5 : margin <= 1;
                        if (isCloseCall) closeCallCount++;
                    }
                }

                closeCallsData.Add(new
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    CloseCallCount = closeCallCount
                });
            }

            return new
            {
                CloseCalls = new
                {
                    DaysAnalyzed = days,
                    Games = closeCallsData,
                    TotalCloseCalls = closeCallsData.Sum(g => (int)((dynamic)g).CloseCallCount)
                }
            };
        }

        private async Task<object> GetStatsDataAsync(DateTime start, DateTime end, int top)
        {
            var dailyChampions = await _context.GameScores
                .AsNoTracking()
                .Include(gs => gs.Game)
                .Where(s => s.DateAchieved >= start && s.DateAchieved < end && s.Game!.IsActive)
                .GroupBy(s => new { s.GameId, s.Game!.Name, s.Game!.ScoringType })
                .Select(g => new
                {
                    g.Key.GameId,
                    g.Key.Name,
                    g.Key.ScoringType,
                    Scores = g.ToList()
                })
                .ToListAsync();

            var champions = dailyChampions.Select(g =>
            {
                GameScore? winner = null;
                if (g.ScoringType == ScoringType.Time)
                {
                    winner = g.Scores.Where(s => s.CompletionTime.HasValue)
                                   .OrderBy(s => s.CompletionTime!.Value)
                                   .FirstOrDefault();
                }
                else
                {
                    winner = g.Scores.Where(s => s.GuessCount.HasValue)
                                   .OrderBy(s => s.GuessCount!.Value)
                                   .FirstOrDefault();
                }

                return new
                {
                    GameId = g.GameId,
                    GameName = g.Name,
                    Winner = winner?.PlayerName,
                    Score = winner != null
                        ? (g.ScoringType == ScoringType.Time
                            ? $"{winner.CompletionTime!.Value.TotalSeconds:F1}s"
                            : $"{winner.GuessCount} guess{(winner.GuessCount == 1 ? "" : "es")}")
                        : "No scores"
                };
            }).ToList();

            return new
            {
                DailyChampions = champions
            };
        }
    }

    public class DailyPageDataDto
    {
        public string Date { get; set; } = string.Empty;
        public Dictionary<int, List<GameScoreDto>> Leaderboards { get; set; } = new();
        public Dictionary<string, object> PlayerTemperatures { get; set; } = new();
        public object Analytics { get; set; } = new();
        public object Stats { get; set; } = new();
    }
}
