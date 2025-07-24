using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using game.api.Data;
using game.api.Models;

namespace game.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly GameContext _context;

        public GamesController(GameContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GameDto>>> GetGames()
        {
            var games = await _context.Games
                .Where(g => g.IsActive)
                .ToListAsync();

            var gameDtos = games.Select(g => new GameDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                CreatedDate = g.CreatedDate,
                IsActive = g.IsActive,
                ScoringType = g.ScoringType
            }).ToList();

            return gameDtos;
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<GameDto>>> GetAllGamesForAdmin()
        {
            var games = await _context.Games.ToListAsync();

            var gameDtos = games.Select(g => new GameDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                CreatedDate = g.CreatedDate,
                IsActive = g.IsActive,
                ScoringType = g.ScoringType
            }).ToList();

            return gameDtos;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GameDto>> GetGame(int id)
        {
            var game = await _context.Games
                .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

            if (game == null)
            {
                return NotFound();
            }

            var gameDto = new GameDto
            {
                Id = game.Id,
                Name = game.Name,
                Description = game.Description,
                CreatedDate = game.CreatedDate,
                IsActive = game.IsActive,
                ScoringType = game.ScoringType
            };

            return gameDto;
        }

        [HttpPost]
        public async Task<ActionResult<Game>> PostGame(Game game)
        {
            game.CreatedDate = DateTime.UtcNow;
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutGame(int id, Game game)
        {
            if (id != game.Id)
            {
                return BadRequest();
            }

            _context.Entry(game).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GameExists(id))
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
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            game.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GameExists(int id)
        {
            return _context.Games.Any(e => e.Id == id);
        }
    }
}