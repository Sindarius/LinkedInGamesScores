using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using game.api.Data;
using game.api.Models;
using game.api.Utils;

namespace game.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly GameContext _context;

        public StatsController(GameContext context)
        {
            _context = context;
        }

        // GET api/stats/daily-champions?date=2025-08-01
        [HttpGet("daily-champions")]
        public async Task<ActionResult<IEnumerable<DailyChampionsDto>>> GetDailyChampions([FromQuery] DateTime? date = null)
        {
            // Normalize to UTC date window [start, end)
            var baseDate = (date?.Date) ?? DateTime.UtcNow.Date;
            var start = DateTime.SpecifyKind(baseDate, DateTimeKind.Utc);
            var end = start.AddDays(1);

            var games = await _context.Games.Where(g => g.IsActive).ToListAsync();

            var scores = await _context.GameScores
                .Include(s => s.Game)
                .Where(s => s.DateAchieved >= start && s.DateAchieved < end)
                .ToListAsync();

            var result = new List<DailyChampionsDto>();

            foreach (var game in games)
            {
                var gScores = scores.Where(s => s.GameId == game.Id);
                IEnumerable<GameScore> winners = Enumerable.Empty<GameScore>();

                if (game.ScoringType == ScoringType.Time)
                {
                    var valid = gScores.Where(s => s.CompletionTime.HasValue).ToList();
                    if (valid.Count > 0)
                    {
                        var best = valid.Min(s => s.CompletionTime!.Value);
                        winners = valid.Where(s => s.CompletionTime!.Value == best);
                    }
                }
                else
                {
                    var valid = gScores.Where(s => s.GuessCount.HasValue).ToList();
                    if (valid.Count > 0)
                    {
                        var best = valid.Min(s => s.GuessCount!.Value);
                        winners = valid.Where(s => s.GuessCount!.Value == best);
                    }
                }

                // Distinct by identity (LinkedIn URL else name) to avoid duplicates
                winners = winners
                    .GroupBy(w => string.IsNullOrWhiteSpace(w.LinkedInProfileUrl)
                        ? w.PlayerName.Trim().ToLowerInvariant()
                        : w.LinkedInProfileUrl!.Trim().ToLowerInvariant())
                    .Select(g => g.First());

                var championList = winners
                    .Select(w => new GameScoreDto
                    {
                        Id = w.Id,
                        GameId = w.GameId,
                        PlayerName = w.PlayerName,
                        GuessCount = w.GuessCount,
                        CompletionTime = w.CompletionTime,
                        Score = game.ScoringType == ScoringType.Time
                            ? (int)(w.CompletionTime.HasValue ? w.CompletionTime.Value.TotalSeconds : 0)
                            : (w.GuessCount ?? 0),
                        DateAchieved = w.DateAchieved,
                        LinkedInProfileUrl = w.LinkedInProfileUrl,
                        GameName = game.Name,
                        ScoringType = game.ScoringType,
                        HasScoreImage = w.ScoreImage != null
                    })
                    .ToList();

                var dto = new DailyChampionsDto
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    ScoringType = game.ScoringType,
                    Champions = championList,
                    Score = championList.FirstOrDefault()?.Score,
                    GuessCount = championList.FirstOrDefault()?.GuessCount,
                    CompletionTime = championList.FirstOrDefault()?.CompletionTime,
                    DateAchieved = championList.FirstOrDefault()?.DateAchieved
                };

                result.Add(dto);
            }

            // Keep a stable order by game name
            result = result.OrderBy(r => r.GameName).ToList();
            return Ok(result);
        }

        // GET api/stats/top-winners?days=7&top=5&gameId=optional
        [HttpGet("top-winners")]
        public async Task<ActionResult<TopWinnersTrendDto>> GetTopWinners([FromQuery] int days = 7, [FromQuery] int top = 5, [FromQuery] int? gameId = null)
        {
            days = Math.Clamp(days, 1, 31);
            top = Math.Clamp(top, 1, 20);

            // Build Pacific-based day windows and labels
            var (utcStart, utcEnd, pacificDays, dateIndex) = TimeZoneHelper.GetRecentPacificWindows(days);
            var labels = pacificDays.Select(d => d.ToString("yyyy-MM-dd")).ToList();

            var query = _context.GameScores
                .Include(gs => gs.Game)
                .Where(gs => gs.DateAchieved >= utcStart && gs.DateAchieved < utcEnd);

            if (gameId.HasValue)
            {
                query = query.Where(gs => gs.GameId == gameId.Value);
            }

            var scores = await query.ToListAsync();

            // Determine daily winners per game, then aggregate across games per day.
            var tz = TimeZoneHelper.GetPacificTimeZone();
            var winnersByDay = new Dictionary<DateTime, List<GameScore>>();

            foreach (var group in scores.GroupBy(s => new { s.GameId, Day = TimeZoneInfo.ConvertTimeFromUtc(s.DateAchieved, tz).Date }))
            {
                var day = group.Key.Day;
                var scoringType = group.First().Game?.ScoringType ?? ScoringType.Guesses;

                // Select all winners (ties) for this game and day
                IEnumerable<GameScore> winnersForGroup = Enumerable.Empty<GameScore>();
                if (scoringType == ScoringType.Time)
                {
                    var valid = group.Where(s => s.CompletionTime.HasValue).ToList();
                    if (valid.Count > 0)
                    {
                        var best = valid.Min(s => s.CompletionTime!.Value);
                        winnersForGroup = valid.Where(s => s.CompletionTime!.Value == best);
                    }
                }
                else
                {
                    var valid = group.Where(s => s.GuessCount.HasValue).ToList();
                    if (valid.Count > 0)
                    {
                        var best = valid.Min(s => s.GuessCount!.Value);
                        winnersForGroup = valid.Where(s => s.GuessCount!.Value == best);
                    }
                }

                if (dateIndex.ContainsKey(day))
                {
                    if (!winnersByDay.TryGetValue(day, out var list))
                    {
                        list = new List<GameScore>();
                        winnersByDay[day] = list;
                    }

                    // Deduplicate winners by identity (LinkedIn URL or name)
                    var distinctWinners = winnersForGroup
                        .GroupBy(w => string.IsNullOrWhiteSpace(w.LinkedInProfileUrl)
                            ? w.PlayerName.Trim().ToLowerInvariant()
                            : w.LinkedInProfileUrl!.Trim().ToLowerInvariant())
                        .Select(g => g.First());

                    list.AddRange(distinctWinners);
                }
            }

            // Aggregate into series per player
            var seriesMap = new Dictionary<string, TopWinnersSeriesDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in winnersByDay)
            {
                var day = kvp.Key;
                var idx = dateIndex[day];
                foreach (var w in kvp.Value)
                {
                    var key = !string.IsNullOrWhiteSpace(w.LinkedInProfileUrl) ? w.LinkedInProfileUrl!.Trim().ToLowerInvariant() : w.PlayerName.Trim().ToLowerInvariant();
                    if (!seriesMap.TryGetValue(key, out var s))
                    {
                        s = new TopWinnersSeriesDto
                        {
                            PlayerId = key,
                            PlayerName = w.PlayerName,
                            ProfileUrl = string.IsNullOrWhiteSpace(w.LinkedInProfileUrl) ? null : w.LinkedInProfileUrl,
                            Data = Enumerable.Repeat(0, days).ToArray()
                        };
                        seriesMap[key] = s;
                    }
                    s.Data[idx] += 1; // may have multiple wins across games in same day
                }
            }

            foreach (var s in seriesMap.Values)
            {
                s.Total = s.Data.Sum();
            }

            var topSeries = seriesMap.Values
                .OrderByDescending(s => s.Total)
                .ThenBy(s => s.PlayerName)
                .Take(top)
                .ToList();

            var result = new TopWinnersTrendDto
            {
                Days = days,
                Labels = labels,
                Series = topSeries
            };

            return Ok(result);
        }
    }
}
