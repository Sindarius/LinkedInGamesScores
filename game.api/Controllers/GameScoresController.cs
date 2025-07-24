using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using game.api.Data;
using game.api.Models;

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
        public async Task<ActionResult<IEnumerable<GameScore>>> GetGameScores()
        {
            return await _context.GameScores
                .Include(gs => gs.Game)
                .OrderByDescending(gs => gs.Score)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GameScore>> GetGameScore(int id)
        {
            var gameScore = await _context.GameScores
                .Include(gs => gs.Game)
                .FirstOrDefaultAsync(gs => gs.Id == id);

            if (gameScore == null)
            {
                return NotFound();
            }

            return gameScore;
        }

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<GameScore>>> GetGameScoresByGame(int gameId)
        {
            return await _context.GameScores
                .Include(gs => gs.Game)
                .Where(gs => gs.GameId == gameId)
                .OrderByDescending(gs => gs.Score)
                .ToListAsync();
        }

        [HttpGet("game/{gameId}/leaderboard")]
        public async Task<ActionResult<IEnumerable<GameScore>>> GetLeaderboard(int gameId, int top = 10)
        {
            return await _context.GameScores
                .Include(gs => gs.Game)
                .Where(gs => gs.GameId == gameId)
                .OrderByDescending(gs => gs.Score)
                .Take(top)
                .ToListAsync();
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