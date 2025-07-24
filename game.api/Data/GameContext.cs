using Microsoft.EntityFrameworkCore;
using game.api.Models;

namespace game.api.Data
{
    public class GameContext : DbContext
    {
        public GameContext(DbContextOptions<GameContext> options) : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<GameScore> GameScores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameScore>()
                .HasOne(gs => gs.Game)
                .WithMany(g => g.Scores)
                .HasForeignKey(gs => gs.GameId);

            modelBuilder.Entity<Game>().HasData(
                new Game
                {
                    Id = 1,
                    Name = "Queens",
                    Description = "LinkedIn Queens puzzle game",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                },
                new Game
                {
                    Id = 2,
                    Name = "Pinpoint",
                    Description = "LinkedIn Pinpoint geography game",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                },
                new Game
                {
                    Id = 3,
                    Name = "Crossclimb",
                    Description = "LinkedIn Crossclimb word ladder game",
                    CreatedDate = DateTime.UtcNow,
                    IsActive = true
                }
            );
        }
    }
}