using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using game.api.Data;
using game.api.Models;
using game.api.Utils;

namespace game.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly GameContext _context;

        public AnalyticsController(GameContext context)
        {
            _context = context;
        }

        [HttpGet("close-calls")]
        public async Task<ActionResult<object>> GetCloseCalls([FromQuery] int days = 7)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var games = await _context.Games.Where(g => g.IsActive).ToListAsync();
                var closeCallsData = new List<object>();

                foreach (var game in games)
                {
                    var scores = await _context.GameScores
                        .Where(gs => gs.GameId == game.Id && gs.DateAchieved >= cutoffDate)
                        .Where(gs => 
                            (game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                            (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
                        .ToListAsync();

                    var dailyGroupedScores = scores
                        .GroupBy(s => s.DateAchieved.Date)
                        .Where(g => g.Count() > 1);

                    int closeCallCount = 0;
                    var closeCallExamples = new List<object>();

                    foreach (var dayGroup in dailyGroupedScores)
                    {
                        List<GameScore> sortedScores;
                        
                        if (game.ScoringType == ScoringType.Time)
                        {
                            sortedScores = dayGroup
                                .OrderBy(s => s.CompletionTime!.Value.TotalSeconds)
                                .ToList();
                        }
                        else
                        {
                            sortedScores = dayGroup
                                .OrderBy(s => s.GuessCount!.Value)
                                .ToList();
                        }

                        for (int i = 0; i < sortedScores.Count - 1; i++)
                        {
                            var current = sortedScores[i];
                            var next = sortedScores[i + 1];
                            
                            bool isCloseCall = false;
                            double margin = 0;
                            
                            if (game.ScoringType == ScoringType.Time)
                            {
                                margin = next.CompletionTime!.Value.TotalSeconds - current.CompletionTime!.Value.TotalSeconds;
                                isCloseCall = margin <= 5; // 5 seconds or less
                            }
                            else if (game.ScoringType == ScoringType.Guesses)
                            {
                                margin = next.GuessCount!.Value - current.GuessCount!.Value;
                                isCloseCall = margin <= 1; // 1 guess difference
                            }

                            if (isCloseCall)
                            {
                                closeCallCount++;
                                if (closeCallExamples.Count < 3)
                                {
                                    closeCallExamples.Add(new
                                    {
                                        Date = dayGroup.Key.ToString("yyyy-MM-dd"),
                                        Winner = current.PlayerName,
                                        RunnerUp = next.PlayerName,
                                        Margin = game.ScoringType == ScoringType.Time 
                                            ? $"{margin:F1}s" 
                                            : $"{margin} guess{(margin == 1 ? "" : "es")}",
                                        WinnerScore = game.ScoringType == ScoringType.Time
                                            ? $"{current.CompletionTime!.Value.TotalSeconds:F1}s"
                                            : $"{current.GuessCount} guess{(current.GuessCount == 1 ? "" : "es")}",
                                        RunnerUpScore = game.ScoringType == ScoringType.Time
                                            ? $"{next.CompletionTime!.Value.TotalSeconds:F1}s"
                                            : $"{next.GuessCount} guess{(next.GuessCount == 1 ? "" : "es")}"
                                    });
                                }
                            }
                        }
                    }

                    closeCallsData.Add(new
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        ScoringType = game.ScoringType,
                        CloseCallCount = closeCallCount,
                        Examples = closeCallExamples
                    });
                }

                return Ok(new
                {
                    DaysAnalyzed = days,
                    Games = closeCallsData,
                    TotalCloseCalls = closeCallsData.Sum(g => (int)((dynamic)g).CloseCallCount)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating close calls: {ex.Message}");
            }
        }

        [HttpGet("comeback-kings")]
        public async Task<ActionResult<object>> GetComebackKings([FromQuery] int days = 14)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var games = await _context.Games.Where(g => g.IsActive).ToListAsync();
                var comebackData = new List<object>();

                foreach (var game in games)
                {
                    var scores = await _context.GameScores
                        .Where(gs => gs.GameId == game.Id && gs.DateAchieved >= cutoffDate)
                        .Where(gs => 
                            (game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                            (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
                        .OrderBy(gs => gs.DateAchieved)
                        .ToListAsync();

                    var playerStreaks = new Dictionary<string, object>();

                    foreach (var playerGroup in scores.GroupBy(s => s.PlayerName.Trim().ToLower()))
                    {
                        var playerScores = playerGroup.OrderBy(s => s.DateAchieved).ToList();
                        if (playerScores.Count < 3) continue; // Need at least 3 scores to calculate improvement

                        var improvements = new List<double>();
                        for (int i = 2; i < playerScores.Count; i++)
                        {
                            var recent3 = playerScores.Skip(i - 2).Take(3).ToList();
                            double improvement = CalculateImprovement(recent3, game.ScoringType);
                            if (improvement > 0) improvements.Add(improvement);
                        }

                        if (improvements.Any())
                        {
                            playerStreaks[playerGroup.Key] = new
                            {
                                PlayerName = playerGroup.First().PlayerName,
                                TotalImprovements = improvements.Count,
                                AverageImprovement = improvements.Average(),
                                MaxImprovement = improvements.Max(),
                                RecentScoresCount = playerScores.Count
                            };
                        }
                    }

                    var topComebbackPlayers = playerStreaks.Values
                        .Cast<dynamic>()
                        .OrderByDescending(p => p.TotalImprovements)
                        .ThenByDescending(p => p.AverageImprovement)
                        .Take(5)
                        .ToList();

                    comebackData.Add(new
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        ScoringType = game.ScoringType,
                        TopPlayers = topComebbackPlayers
                    });
                }

                return Ok(new
                {
                    DaysAnalyzed = days,
                    Games = comebackData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating comeback kings: {ex.Message}");
            }
        }

        [HttpGet("consistency-champions")]
        public async Task<ActionResult<object>> GetConsistencyChampions([FromQuery] int days = 30, [FromQuery] int minScores = 5)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var games = await _context.Games.Where(g => g.IsActive).ToListAsync();
                var consistencyData = new List<object>();

                foreach (var game in games)
                {
                    var scores = await _context.GameScores
                        .Where(gs => gs.GameId == game.Id && gs.DateAchieved >= cutoffDate)
                        .Where(gs => 
                            (game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                            (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
                        .ToListAsync();

                    var playerConsistency = new List<object>();

                    foreach (var playerGroup in scores.GroupBy(s => s.PlayerName.Trim().ToLower()))
                    {
                        var playerScores = playerGroup.ToList();
                        if (playerScores.Count < minScores) continue;

                        var values = new List<double>();
                        foreach (var score in playerScores)
                        {
                            if (game.ScoringType == ScoringType.Time)
                            {
                                values.Add(score.CompletionTime!.Value.TotalSeconds);
                            }
                            else
                            {
                                values.Add(score.GuessCount!.Value);
                            }
                        }

                        var mean = values.Average();
                        var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
                        var stdDev = Math.Sqrt(variance);
                        var coefficientOfVariation = mean > 0 ? (stdDev / mean) * 100 : 0;

                        playerConsistency.Add(new
                        {
                            PlayerName = playerGroup.First().PlayerName,
                            ScoreCount = playerScores.Count,
                            Mean = mean,
                            StandardDeviation = stdDev,
                            CoefficientOfVariation = coefficientOfVariation,
                            BestScore = game.ScoringType == ScoringType.Time 
                                ? values.Min() 
                                : values.Min(),
                            WorstScore = game.ScoringType == ScoringType.Time 
                                ? values.Max() 
                                : values.Max()
                        });
                    }

                    var topConsistentPlayers = playerConsistency
                        .Cast<dynamic>()
                        .OrderBy(p => p.CoefficientOfVariation)
                        .Take(10)
                        .ToList();

                    consistencyData.Add(new
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        ScoringType = game.ScoringType,
                        TopPlayers = topConsistentPlayers
                    });
                }

                return Ok(new
                {
                    DaysAnalyzed = days,
                    MinimumScores = minScores,
                    Games = consistencyData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating consistency champions: {ex.Message}");
            }
        }

        [HttpGet("distribution/{scoringType}")]
        public async Task<ActionResult<object>> GetScoreDistribution(ScoringType scoringType, [FromQuery] int days = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var games = await _context.Games
                    .Where(g => g.IsActive && g.ScoringType == scoringType)
                    .ToListAsync();

                var distributionData = new List<object>();

                foreach (var game in games)
                {
                    var scores = await _context.GameScores
                        .Where(gs => gs.GameId == game.Id && gs.DateAchieved >= cutoffDate)
                        .Where(gs => 
                            (scoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                            (scoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
                        .ToListAsync();

                    Dictionary<string, int> distribution;

                    if (scoringType == ScoringType.Time)
                    {
                        distribution = new Dictionary<string, int>
                        {
                            ["0-30s"] = 0,
                            ["31-60s"] = 0,
                            ["61-120s"] = 0,
                            ["121-300s"] = 0,
                            ["300s+"] = 0
                        };

                        foreach (var score in scores.Where(s => s.CompletionTime.HasValue))
                        {
                            var seconds = score.CompletionTime!.Value.TotalSeconds;
                            if (seconds <= 30) distribution["0-30s"]++;
                            else if (seconds <= 60) distribution["31-60s"]++;
                            else if (seconds <= 120) distribution["61-120s"]++;
                            else if (seconds <= 300) distribution["121-300s"]++;
                            else distribution["300s+"]++;
                        }
                    }
                    else // Guesses
                    {
                        distribution = new Dictionary<string, int>
                        {
                            ["1"] = 0,
                            ["2"] = 0,
                            ["3"] = 0,
                            ["4"] = 0,
                            ["5"] = 0,
                            ["6+"] = 0
                        };

                        foreach (var score in scores.Where(s => s.GuessCount.HasValue))
                        {
                            var guesses = score.GuessCount!.Value;
                            if (guesses <= 5)
                            {
                                distribution[guesses.ToString()]++;
                            }
                            else
                            {
                                distribution["6+"]++;
                            }
                        }
                    }

                    distributionData.Add(new
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        ScoringType = scoringType,
                        TotalScores = scores.Count,
                        Distribution = distribution
                    });
                }

                return Ok(new
                {
                    ScoringType = scoringType,
                    DaysAnalyzed = days,
                    Games = distributionData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating score distribution: {ex.Message}");
            }
        }

        [HttpGet("photo-finish")]
        public async Task<ActionResult<object>> GetPhotoFinishes([FromQuery] DateTime? date = null)
        {
            try
            {
                var (start, end, _) = TimeZoneHelper.GetPacificDayRange(date);
                var games = await _context.Games.Where(g => g.IsActive).ToListAsync();
                var photoFinishes = new List<object>();

                foreach (var game in games)
                {
                    var dailyScores = await _context.GameScores
                        .Where(gs => gs.GameId == game.Id && gs.DateAchieved >= start && gs.DateAchieved < end)
                        .Where(gs => 
                            (game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                            (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
                        .ToListAsync();

                    if (dailyScores.Count < 2) continue;

                    List<GameScore> sortedScores;
                    if (game.ScoringType == ScoringType.Time)
                    {
                        sortedScores = dailyScores.OrderBy(s => s.CompletionTime!.Value.TotalSeconds).ToList();
                    }
                    else
                    {
                        sortedScores = dailyScores.OrderBy(s => s.GuessCount!.Value).ToList();
                    }

                    // Check for photo finish (tight race for top positions)
                    bool isPhotoFinish = false;
                    double margin = 0;
                    
                    if (sortedScores.Count >= 2)
                    {
                        var first = sortedScores[0];
                        var second = sortedScores[1];
                        
                        if (game.ScoringType == ScoringType.Time)
                        {
                            margin = second.CompletionTime!.Value.TotalSeconds - first.CompletionTime!.Value.TotalSeconds;
                            isPhotoFinish = margin <= 3; // 3 seconds or less
                        }
                        else
                        {
                            margin = second.GuessCount!.Value - first.GuessCount!.Value;
                            isPhotoFinish = margin == 0; // Same number of guesses (tie)
                        }
                    }

                    if (isPhotoFinish)
                    {
                        photoFinishes.Add(new
                        {
                            GameId = game.Id,
                            GameName = game.Name,
                            ScoringType = game.ScoringType,
                            Date = start.ToString("yyyy-MM-dd"),
                            Leader = sortedScores[0].PlayerName,
                            RunnerUp = sortedScores[1].PlayerName,
                            Margin = game.ScoringType == ScoringType.Time 
                                ? $"{margin:F1}s" 
                                : margin == 0 ? "TIE" : $"{margin} guess{(margin == 1 ? "" : "es")}",
                            LeaderScore = game.ScoringType == ScoringType.Time
                                ? $"{sortedScores[0].CompletionTime!.Value.TotalSeconds:F1}s"
                                : $"{sortedScores[0].GuessCount} guess{(sortedScores[0].GuessCount == 1 ? "" : "es")}",
                            RunnerUpScore = game.ScoringType == ScoringType.Time
                                ? $"{sortedScores[1].CompletionTime!.Value.TotalSeconds:F1}s"
                                : $"{sortedScores[1].GuessCount} guess{(sortedScores[1].GuessCount == 1 ? "" : "es")}",
                            TotalParticipants = dailyScores.Count
                        });
                    }
                }

                return Ok(new
                {
                    Date = start.ToString("yyyy-MM-dd"),
                    PhotoFinishes = photoFinishes,
                    TotalPhotoFinishes = photoFinishes.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error detecting photo finishes: {ex.Message}");
            }
        }

        [HttpGet("player-temperature/{playerName}")]
        public async Task<ActionResult<object>> GetPlayerTemperature(string playerName, [FromQuery] int days = 7)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var normalizedPlayerName = playerName.Trim().ToLower();
                var games = await _context.Games.Where(g => g.IsActive).ToListAsync();
                var temperatureData = new List<object>();

                foreach (var game in games)
                {
                    var playerScores = await _context.GameScores
                        .Where(gs => gs.GameId == game.Id && gs.DateAchieved >= cutoffDate)
                        .Where(gs => gs.PlayerName.Trim().ToLower() == normalizedPlayerName)
                        .Where(gs => 
                            (game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                            (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
                        .OrderBy(gs => gs.DateAchieved)
                        .ToListAsync();

                    if (playerScores.Count == 0) continue;

                    var trend = CalculateTrend(playerScores, game.ScoringType);
                    var recentPerformance = CalculateRecentPerformance(playerScores, game.ScoringType);

                    temperatureData.Add(new
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        ScoringType = game.ScoringType,
                        ScoreCount = playerScores.Count,
                        Trend = trend, // "Improving", "Declining", "Stable"
                        Temperature = recentPerformance, // "Hot", "Warm", "Cool", "Cold"
                        LatestScore = GetFormattedScore(playerScores.Last(), game.ScoringType),
                        BestScore = GetFormattedScore(GetBestScore(playerScores, game.ScoringType), game.ScoringType)
                    });
                }

                return Ok(new
                {
                    PlayerName = playerName,
                    DaysAnalyzed = days,
                    Games = temperatureData,
                    OverallTemperature = CalculateOverallTemperature(temperatureData.Cast<dynamic>().ToList())
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error calculating player temperature: {ex.Message}");
            }
        }

        private double CalculateImprovement(List<GameScore> recentScores, ScoringType scoringType)
        {
            if (recentScores.Count < 3) return 0;

            var values = new List<double>();
            foreach (var score in recentScores)
            {
                if (scoringType == ScoringType.Time)
                {
                    values.Add(score.CompletionTime!.Value.TotalSeconds);
                }
                else
                {
                    values.Add(score.GuessCount!.Value);
                }
            }

            var firstAvg = values.Take(2).Average();
            var latestScore = values.Last();

            // For both time and guesses, lower is better, so improvement is firstAvg - latestScore
            return firstAvg - latestScore;
        }

        private string CalculateTrend(List<GameScore> scores, ScoringType scoringType)
        {
            if (scores.Count < 3) return "Stable";

            var values = new List<double>();
            foreach (var score in scores)
            {
                if (scoringType == ScoringType.Time)
                {
                    values.Add(score.CompletionTime!.Value.TotalSeconds);
                }
                else
                {
                    values.Add(score.GuessCount!.Value);
                }
            }

            var firstHalf = values.Take(values.Count / 2).Average();
            var secondHalf = values.Skip(values.Count / 2).Average();

            var improvement = firstHalf - secondHalf;
            var threshold = scoringType == ScoringType.Time ? 5.0 : 0.5; // 5 seconds or 0.5 guesses

            if (improvement > threshold) return "Improving";
            if (improvement < -threshold) return "Declining";
            return "Stable";
        }

        private string CalculateRecentPerformance(List<GameScore> scores, ScoringType scoringType)
        {
            if (scores.Count == 0) return "Cold";

            var recentScores = scores.TakeLast(Math.Min(3, scores.Count)).ToList();
            var allTimeScores = scores.ToList();

            var recentAvg = CalculateAverageScore(recentScores, scoringType);
            var allTimeAvg = CalculateAverageScore(allTimeScores, scoringType);
            var bestScore = GetBestScoreValue(allTimeScores, scoringType);

            // Calculate how close recent performance is to personal best
            var improvementFromAvg = allTimeAvg - recentAvg;
            var improvementFromBest = bestScore - recentAvg;

            var avgThreshold = scoringType == ScoringType.Time ? 10.0 : 1.0;
            var bestThreshold = scoringType == ScoringType.Time ? 5.0 : 0.5;

            if (Math.Abs(improvementFromBest) <= bestThreshold) return "Hot";
            if (improvementFromAvg > avgThreshold) return "Warm";
            if (Math.Abs(improvementFromAvg) <= avgThreshold) return "Cool";
            return "Cold";
        }

        private double CalculateAverageScore(List<GameScore> scores, ScoringType scoringType)
        {
            if (scoringType == ScoringType.Time)
            {
                return scores.Average(s => s.CompletionTime!.Value.TotalSeconds);
            }
            return scores.Average(s => s.GuessCount!.Value);
        }

        private double GetBestScoreValue(List<GameScore> scores, ScoringType scoringType)
        {
            if (scoringType == ScoringType.Time)
            {
                return scores.Min(s => s.CompletionTime!.Value.TotalSeconds);
            }
            return scores.Min(s => s.GuessCount!.Value);
        }

        private GameScore GetBestScore(List<GameScore> scores, ScoringType scoringType)
        {
            if (scoringType == ScoringType.Time)
            {
                return scores.OrderBy(s => s.CompletionTime!.Value.TotalSeconds).First();
            }
            return scores.OrderBy(s => s.GuessCount!.Value).First();
        }

        private string GetFormattedScore(GameScore score, ScoringType scoringType)
        {
            if (scoringType == ScoringType.Time)
            {
                return $"{score.CompletionTime!.Value.TotalSeconds:F1}s";
            }
            return $"{score.GuessCount} guess{(score.GuessCount == 1 ? "" : "es")}";
        }

        private string CalculateOverallTemperature(List<dynamic> gameTemperatures)
        {
            if (!gameTemperatures.Any()) return "Cold";

            var temperatureCounts = new Dictionary<string, int>();
            foreach (var game in gameTemperatures)
            {
                var temp = (string)game.Temperature;
                temperatureCounts[temp] = temperatureCounts.ContainsKey(temp) ? temperatureCounts[temp] + 1 : 1;
            }

            var dominantTemp = temperatureCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            return dominantTemp;
        }
    }
}