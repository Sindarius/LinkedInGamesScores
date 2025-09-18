using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using game.api.Data;
using game.api.Models;
using game.api.Attributes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using game.api.Utils;

namespace game.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameScoresController : ControllerBase
    {
        private readonly GameContext _context;

        public GameScoresController(GameContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameScoreDto>>> GetGameScores()
        {
            var gameScoreDtos = await _context.GameScores
                .Include(gs => gs.Game)
                .OrderBy(gs => gs.Game!.ScoringType == ScoringType.Time 
                    ? (gs.CompletionTime.HasValue ? (int)gs.CompletionTime.Value.TotalSeconds : 0) 
                    : 0)
                .ThenByDescending(gs => gs.Game!.ScoringType == ScoringType.Guesses 
                    ? (gs.GuessCount ?? 0) 
                    : 0)
                .Select(gs => new GameScoreDto
                {
                    Id = gs.Id,
                    GameId = gs.GameId,
                    PlayerName = gs.PlayerName,
                    GuessCount = gs.GuessCount,
                    CompletionTime = gs.CompletionTime,
                    Score = gs.Game!.ScoringType == ScoringType.Time 
                        ? (int)(gs.CompletionTime.HasValue ? gs.CompletionTime.Value.TotalSeconds : 0)
                        : (gs.GuessCount ?? 0),
                    DateAchieved = gs.DateAchieved,
                    LinkedInProfileUrl = gs.LinkedInProfileUrl,
                    GameName = gs.Game!.Name,
                    ScoringType = gs.Game!.ScoringType,
                    HasScoreImage = gs.ScoreImage != null
                })
                .ToListAsync();

            return gameScoreDtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GameScoreDto>> GetGameScore(int id)
        {
            var gameScoreDto = await _context.GameScores
                .Include(gs => gs.Game)
                .Where(gs => gs.Id == id)
                .Select(gs => new GameScoreDto
                {
                    Id = gs.Id,
                    GameId = gs.GameId,
                    PlayerName = gs.PlayerName,
                    GuessCount = gs.GuessCount,
                    CompletionTime = gs.CompletionTime,
                    Score = gs.Game!.ScoringType == ScoringType.Time 
                        ? (int)(gs.CompletionTime.HasValue ? gs.CompletionTime.Value.TotalSeconds : 0)
                        : (gs.GuessCount ?? 0),
                    DateAchieved = gs.DateAchieved,
                    LinkedInProfileUrl = gs.LinkedInProfileUrl,
                    GameName = gs.Game!.Name,
                    ScoringType = gs.Game!.ScoringType,
                    HasScoreImage = gs.ScoreImage != null
                })
                .FirstOrDefaultAsync();

            if (gameScoreDto == null)
            {
                return NotFound();
            }

            return gameScoreDto;
        }

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<GameScoreDto>>> GetGameScoresByGame(int gameId)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            var gameScores = await _context.GameScores
                .Where(gs => gs.GameId == gameId)
                .ToListAsync();

            var gameScoreDtos = gameScores
                .Where(gs => 
                    (game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                    (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
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
                    HasScoreImage = gs.ScoreImage != null
                })
                .OrderBy(gs => game.ScoringType == ScoringType.Time ? gs.Score : 0)
                .ThenByDescending(gs => game.ScoringType == ScoringType.Guesses ? gs.Score : 0)
                .ToList();

            return gameScoreDtos;
        }

        [HttpGet("game/{gameId}/leaderboard")]
        public async Task<ActionResult<IEnumerable<GameScoreDto>>> GetLeaderboard(int gameId, int top = 10)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            var gameScores = await _context.GameScores
                .Where(gs => gs.GameId == gameId)
                .ToListAsync();

            var gameScoreDtos = gameScores
                .Where(gs => 
                    (game.ScoringType == ScoringType.Time && gs.CompletionTime.HasValue) ||
                    (game.ScoringType == ScoringType.Guesses && gs.GuessCount.HasValue))
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
                    HasScoreImage = gs.ScoreImage != null
                })
                .OrderBy(gs => game.ScoringType == ScoringType.Time ? gs.Score : 0)
                .ThenByDescending(gs => game.ScoringType == ScoringType.Guesses ? gs.Score : 0)
                .Take(top)
                .ToList();

            return gameScoreDtos;
        }

        [HttpGet("game/{gameId}/leaderboard/day")]
        public async Task<ActionResult<IEnumerable<GameScoreDto>>> GetDailyLeaderboard(int gameId, [FromQuery] DateTime? date = null, int top = 10)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) return NotFound();

            // Use Pacific day boundaries
            var (start, end, _) = TimeZoneHelper.GetPacificDayRange(date);

            var query = _context.GameScores
                .Where(gs => gs.GameId == gameId && gs.DateAchieved >= start && gs.DateAchieved < end);

            if (game.ScoringType == ScoringType.Time)
            {
                query = query.Where(gs => gs.CompletionTime.HasValue)
                             .OrderBy(gs => gs.CompletionTime);
            }
            else
            {
                query = query.Where(gs => gs.GuessCount.HasValue)
                             .OrderBy(gs => gs.GuessCount);
            }

            var results = await query
                .Select(gs => new GameScoreDto
                {
                    Id = gs.Id,
                    GameId = gs.GameId,
                    PlayerName = gs.PlayerName,
                    GuessCount = gs.GuessCount,
                    CompletionTime = gs.CompletionTime,
                    Score = game.ScoringType == ScoringType.Time
                        ? (int)(gs.CompletionTime.HasValue ? gs.CompletionTime.Value.TotalSeconds : 0)
                        : (gs.GuessCount ?? 0),
                    DateAchieved = gs.DateAchieved,
                    LinkedInProfileUrl = gs.LinkedInProfileUrl,
                    GameName = game.Name,
                    ScoringType = game.ScoringType,
                    HasScoreImage = gs.ScoreImage != null
                })
                .Take(top)
                .ToListAsync();

            return results;
        }

        [HttpPost("with-image")]
        public async Task<ActionResult<GameScore>> PostGameScoreWithImage([FromForm] GameScoreWithImageDto dto)
        {
            var gameScore = new GameScore
            {
                GameId = dto.GameId,
                PlayerName = dto.PlayerName,
                GuessCount = dto.GuessCount,
                CompletionTime = dto.CompletionTime,
                LinkedInProfileUrl = dto.LinkedInProfileUrl,
                DateAchieved = DateTime.UtcNow
            };

            if (dto.ScoreImage != null && dto.ScoreImage.Length > 0)
            {
                var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedContentTypes.Contains(dto.ScoreImage.ContentType.ToLower()))
                {
                    return BadRequest("Only JPEG, PNG, and GIF images are allowed.");
                }

                if (dto.ScoreImage.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    return BadRequest("Image size cannot exceed 5MB.");
                }

                using var memoryStream = new MemoryStream();
                await dto.ScoreImage.CopyToAsync(memoryStream);
                gameScore.ScoreImage = memoryStream.ToArray();
                gameScore.ImageContentType = dto.ScoreImage.ContentType;
            }

            _context.GameScores.Add(gameScore);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGameScore), new { id = gameScore.Id }, gameScore);
        }

        [HttpGet("{id}/image")]
        public async Task<IActionResult> GetGameScoreImage(int id)
        {
            var gameScore = await _context.GameScores.FindAsync(id);
            if (gameScore == null || gameScore.ScoreImage == null)
            {
                return NotFound();
            }

            return File(gameScore.ScoreImage, gameScore.ImageContentType ?? "image/jpeg");
        }

        [HttpGet("{id}/image/thumbnail")]
        public async Task<IActionResult> GetGameScoreImageThumbnail(int id, [FromQuery] int width = 200, [FromQuery] int height = 150)
        {
            var gameScore = await _context.GameScores.FindAsync(id);
            if (gameScore == null || gameScore.ScoreImage == null)
            {
                return NotFound();
            }

            try
            {
                using var image = Image.Load(gameScore.ScoreImage);
                
                // Resize image maintaining aspect ratio
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max // Maintains aspect ratio, fits within bounds
                }));

                using var output = new MemoryStream();
                await image.SaveAsJpegAsync(output);
                
                return File(output.ToArray(), "image/jpeg");
            }
            catch (Exception ex)
            {
                // Log the error (in production, you'd use proper logging)
                Console.WriteLine($"Error generating thumbnail: {ex.Message}");
                return StatusCode(500, "Error generating thumbnail");
            }
        }

        [HttpPost]
        public async Task<ActionResult<GameScore>> PostGameScore(GameScore gameScore)
        {
            gameScore.DateAchieved = DateTime.UtcNow;
            _context.GameScores.Add(gameScore);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGameScore), new { id = gameScore.Id }, gameScore);
        }

        [HttpPut("{id}")]
        [AdminAuthorize]
        public async Task<IActionResult> PutGameScore(int id, GameScore gameScore)
        {
            if (id != gameScore.Id)
            {
                return BadRequest();
            }

            _context.Entry(gameScore).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GameScoreExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [AdminAuthorize]
        public async Task<IActionResult> DeleteGameScore(int id)
        {
            var gameScore = await _context.GameScores.FindAsync(id);
            if (gameScore == null)
            {
                return NotFound();
            }

            _context.GameScores.Remove(gameScore);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GameScoreExists(int id)
        {
            return _context.GameScores.Any(e => e.Id == id);
        }
    }
}
