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
            // Use Pacific day boundaries
            var (start, end, _) = TimeZoneHelper.GetPacificDayRange(date);

            var games = await _context.Games.AsNoTracking().Where(g => g.IsActive).ToListAsync();

            var scores = await _context.GameScores
                .AsNoTracking()
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
                        HasScoreImage = w.Image != null || w.ScoreImage != null
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

        // GET api/stats/streaks?date=2026-05-09
        [HttpGet("streaks")]
        public async Task<ActionResult<Dictionary<string, PlayerStreakDto>>> GetStreaks([FromQuery] DateTime? date = null)
        {
            var tz = TimeZoneHelper.GetPacificTimeZone();
            var (start, end, pacificDate) = TimeZoneHelper.GetPacificDayRange(date);

            // Players who submitted on the requested date
            var todayScores = await _context.GameScores
                .AsNoTracking()
                .Where(s => s.DateAchieved >= start && s.DateAchieved < end)
                .ToListAsync();

            // Unique player identities (LinkedIn URL preferred, else name)
            var identities = todayScores
                .GroupBy(s => string.IsNullOrWhiteSpace(s.LinkedInProfileUrl)
                    ? s.PlayerName.Trim().ToLowerInvariant()
                    : s.LinkedInProfileUrl!.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            if (identities.Count == 0)
                return Ok(new Dictionary<string, PlayerStreakDto>());

            // Load all historical scores for these players in one query
            var linkedInUrls = identities
                .Where(p => !string.IsNullOrWhiteSpace(p.LinkedInProfileUrl))
                .Select(p => p.LinkedInProfileUrl!.Trim().ToLowerInvariant())
                .ToList();

            var playerNames = identities
                .Where(p => string.IsNullOrWhiteSpace(p.LinkedInProfileUrl))
                .Select(p => p.PlayerName.Trim().ToLowerInvariant())
                .ToList();

            var allScores = await _context.GameScores
                .AsNoTracking()
                .Where(s =>
                    (s.LinkedInProfileUrl != null && linkedInUrls.Contains(s.LinkedInProfileUrl.ToLower())) ||
                    (string.IsNullOrWhiteSpace(s.LinkedInProfileUrl) && playerNames.Contains(s.PlayerName.ToLower())))
                .Select(s => new { s.LinkedInProfileUrl, s.PlayerName, s.DateAchieved })
                .ToListAsync();

            var result = new Dictionary<string, PlayerStreakDto>();

            foreach (var identity in identities)
            {
                var hasUrl = !string.IsNullOrWhiteSpace(identity.LinkedInProfileUrl);
                var key = hasUrl
                    ? identity.LinkedInProfileUrl!.Trim().ToLowerInvariant()
                    : identity.PlayerName.Trim().ToLowerInvariant();

                var playerDates = allScores
                    .Where(s => hasUrl
                        ? s.LinkedInProfileUrl?.Trim().ToLowerInvariant() == key
                        : string.IsNullOrWhiteSpace(s.LinkedInProfileUrl) && s.PlayerName.Trim().ToLowerInvariant() == key)
                    .Select(s => TimeZoneInfo.ConvertTimeFromUtc(s.DateAchieved, tz).Date)
                    .Distinct()
                    .OrderByDescending(d => d)
                    .ToList();

                var (currentStreak, bestStreak) = ComputeStreaks(playerDates, pacificDate);

                result[key] = new PlayerStreakDto
                {
                    PlayerName = identity.PlayerName,
                    LinkedInProfileUrl = identity.LinkedInProfileUrl,
                    CurrentStreak = currentStreak,
                    BestStreak = bestStreak
                };
            }

            return Ok(result);
        }

        // GET api/stats/player?playerName=Juan&linkedInUrl=https://...
        [HttpGet("player")]
        public async Task<ActionResult<PlayerStatsDto>> GetPlayerStats(
            [FromQuery] string playerName,
            [FromQuery] string? linkedInUrl = null)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return BadRequest("playerName is required");

            var tz = TimeZoneHelper.GetPacificTimeZone();
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            var historyStart = today.AddDays(-13);

            var normalizedName = playerName.Trim().ToLowerInvariant();
            var normalizedUrl = linkedInUrl?.Trim().ToLowerInvariant();

            // Load all of this player's scores
            IQueryable<GameScore> query = _context.GameScores
                .AsNoTracking()
                .Include(s => s.Game)
                .Include(s => s.Image);

            if (!string.IsNullOrWhiteSpace(normalizedUrl))
                query = query.Where(s => s.LinkedInProfileUrl != null && s.LinkedInProfileUrl.ToLower() == normalizedUrl);
            else
                query = query.Where(s => s.PlayerName.ToLower() == normalizedName);

            var playerScores = await query.ToListAsync();

            if (playerScores.Count == 0)
                return NotFound("Player not found");

            var games = await _context.Games.AsNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();
            var gameIds = playerScores.Select(s => s.GameId).Distinct().ToList();

            // Load all other players' scores for the same games (for rank computation)
            var allGameScores = await _context.GameScores
                .AsNoTracking()
                .Where(s => gameIds.Contains(s.GameId))
                .Select(s => new { s.GameId, s.PlayerName, s.LinkedInProfileUrl, s.GuessCount, s.CompletionTime, s.DateAchieved })
                .ToListAsync();

            // Streak
            var distinctDates = playerScores
                .Select(s => TimeZoneInfo.ConvertTimeFromUtc(s.DateAchieved, tz).Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();
            var (currentStreak, bestStreak) = ComputeStreaks(distinctDates, today);

            // Per-game stats
            var gameStatsList = new List<PlayerGameStatsDto>();
            var recentResults = new List<RecentResultDto>();

            foreach (var game in games)
            {
                var gamePlayerScores = playerScores.Where(s => s.GameId == game.Id).ToList();
                if (gamePlayerScores.Count == 0) continue;

                // Best score per Pacific day
                var byDay = gamePlayerScores
                    .GroupBy(s => TimeZoneInfo.ConvertTimeFromUtc(s.DateAchieved, tz).Date)
                    .Select(g =>
                    {
                        var best = game.ScoringType == ScoringType.Time
                            ? g.Where(s => s.CompletionTime.HasValue).OrderBy(s => s.CompletionTime!.Value).FirstOrDefault()
                            : g.Where(s => s.GuessCount.HasValue).OrderBy(s => s.GuessCount!.Value).FirstOrDefault();
                        return (Date: g.Key, Score: best);
                    })
                    .Where(x => x.Score != null)
                    .ToList();

                int wins = 0;
                double rankSum = 0;
                double scoreSum = 0;
                double? bestScore = null;
                bool neverDnf = true;
                var rankHistory = new List<RankHistoryEntryDto>();

                foreach (var (date, score) in byDay)
                {
                    double playerVal;
                    if (game.ScoringType == ScoringType.Time)
                    {
                        if (!score!.CompletionTime.HasValue) continue;
                        playerVal = score.CompletionTime.Value.TotalSeconds;
                    }
                    else
                    {
                        if (!score!.GuessCount.HasValue) continue;
                        if (score.GuessCount.Value == 99) neverDnf = false;
                        playerVal = score.GuessCount.Value == 99 ? double.MaxValue : score.GuessCount.Value;
                    }

                    // Count how many players scored strictly better on that day
                    var dayOthers = allGameScores
                        .Where(s => s.GameId == game.Id &&
                                    TimeZoneInfo.ConvertTimeFromUtc(s.DateAchieved, tz).Date == date)
                        .ToList();

                    int rank;
                    if (game.ScoringType == ScoringType.Time)
                        rank = dayOthers.Count(s => s.CompletionTime.HasValue && s.CompletionTime.Value.TotalSeconds < playerVal) + 1;
                    else
                        rank = dayOthers.Count(s => s.GuessCount.HasValue && s.GuessCount.Value != 99 && s.GuessCount.Value < playerVal) + 1;

                    if (rank == 1) wins++;
                    rankSum += rank;
                    if (playerVal != double.MaxValue)
                    {
                        scoreSum += playerVal;
                        if (bestScore == null || playerVal < bestScore) bestScore = playerVal;
                    }

                    if (date >= historyStart)
                        rankHistory.Add(new RankHistoryEntryDto { Date = date.ToString("yyyy-MM-dd"), Rank = rank, Score = playerVal == double.MaxValue ? null : playerVal });

                    recentResults.Add(new RecentResultDto
                    {
                        ScoreId = score.Id,
                        GameId = game.Id,
                        GameName = game.Name,
                        ScoringType = game.ScoringType,
                        Date = date.ToString("yyyy-MM-dd"),
                        GuessCount = score.GuessCount,
                        CompletionTime = score.CompletionTime,
                        Score = playerVal == double.MaxValue ? 99 : playerVal,
                        Rank = rank,
                        IsDnf = game.ScoringType == ScoringType.Guesses && score.GuessCount == 99,
                        HasImage = score.Image != null || score.ScoreImage != null
                    });
                }

                // Fill missing days in rank history with null (didn't play)
                for (var d = historyStart; d <= today; d = d.AddDays(1))
                {
                    var label = d.ToString("yyyy-MM-dd");
                    if (!rankHistory.Any(h => h.Date == label))
                        rankHistory.Add(new RankHistoryEntryDto { Date = label, Rank = null, Score = null });
                }

                gameStatsList.Add(new PlayerGameStatsDto
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    ScoringType = game.ScoringType,
                    TotalGames = byDay.Count,
                    Wins = wins,
                    WinRate = byDay.Count > 0 ? Math.Round((double)wins / byDay.Count * 100, 1) : 0,
                    AvgRank = byDay.Count > 0 ? Math.Round(rankSum / byDay.Count, 2) : 0,
                    AvgScore = byDay.Count > 0 && scoreSum > 0 ? Math.Round(scoreSum / byDay.Count, 2) : null,
                    BestScore = bestScore,
                    NeverDnf = neverDnf,
                    RankHistory = rankHistory.OrderBy(h => h.Date).ToList()
                });
            }

            recentResults = recentResults
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.GameName)
                .Take(20)
                .ToList();

            // Achievements
            var achievements = new List<string>();
            if (currentStreak >= 3)
                achievements.Add($"🔥 {currentStreak}-day streak");
            if (bestStreak >= 14)
                achievements.Add($"🏆 Best streak: {bestStreak} days");
            foreach (var gs in gameStatsList.Where(g => g.NeverDnf && g.TotalGames >= 3))
                achievements.Add($"💯 Never DNF'd in {gs.GameName}");
            foreach (var gs in gameStatsList.Where(g => g.WinRate >= 50 && g.TotalGames >= 5))
                achievements.Add($"👑 {gs.WinRate:F0}% win rate in {gs.GameName}");

            var canonical = playerScores.OrderByDescending(s => s.DateAchieved).First();

            return Ok(new PlayerStatsDto
            {
                PlayerName = canonical.PlayerName,
                LinkedInProfileUrl = canonical.LinkedInProfileUrl,
                TotalGames = gameStatsList.Sum(g => g.TotalGames),
                CurrentStreak = currentStreak,
                BestStreak = bestStreak,
                Achievements = achievements,
                GameStats = gameStatsList,
                RecentResults = recentResults
            });
        }

        private static (int current, int best) ComputeStreaks(List<DateTime> sortedDesc, DateTime referenceDate)
        {
            if (sortedDesc.Count == 0) return (0, 0);

            // Current streak: consecutive days ending on referenceDate or referenceDate-1
            int currentStreak = 0;
            var mostRecent = sortedDesc[0];
            if (mostRecent == referenceDate || mostRecent == referenceDate.AddDays(-1))
            {
                currentStreak = 1;
                var expected = mostRecent.AddDays(-1);
                for (int i = 1; i < sortedDesc.Count; i++)
                {
                    if (sortedDesc[i] == expected) { currentStreak++; expected = expected.AddDays(-1); }
                    else break;
                }
            }

            // Best streak: longest consecutive run in entire history
            int bestStreak = 1;
            int run = 1;
            for (int i = 1; i < sortedDesc.Count; i++)
            {
                if (sortedDesc[i] == sortedDesc[i - 1].AddDays(-1)) { run++; bestStreak = Math.Max(bestStreak, run); }
                else run = 1;
            }
            bestStreak = Math.Max(bestStreak, currentStreak);

            return (currentStreak, bestStreak);
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
                .AsNoTracking()
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
