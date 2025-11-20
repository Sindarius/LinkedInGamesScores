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
        public DbSet<GameScoreImage> GameScoreImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure GameScore to Game relationship
            modelBuilder.Entity<GameScore>()
                .HasOne(gs => gs.Game)
                .WithMany(g => g.Scores)
                .HasForeignKey(gs => gs.GameId);

            // Configure GameScoreImage to GameScore relationship (1-to-1)
            modelBuilder.Entity<GameScoreImage>()
                .HasOne(gsi => gsi.GameScore)
                .WithOne(gs => gs.Image)
                .HasForeignKey<GameScoreImage>(gsi => gsi.GameScoreId)
                .OnDelete(DeleteBehavior.Cascade);

            // Performance indexes for common query patterns
            // Frequently filter/sort by date and game
            modelBuilder.Entity<GameScore>()
                .HasIndex(gs => gs.DateAchieved)
                .HasDatabaseName("IX_GameScores_DateAchieved");

            modelBuilder.Entity<GameScore>()
                .HasIndex(gs => new { gs.GameId, gs.DateAchieved })
                .HasDatabaseName("IX_GameScores_GameId_DateAchieved");

            // Index on GameScoreImage for quick lookups
            modelBuilder.Entity<GameScoreImage>()
                .HasIndex(gsi => gsi.GameScoreId)
                .IsUnique()
                .HasDatabaseName("IX_GameScoreImages_GameScoreId");

            modelBuilder.Entity<Game>().HasData(
                new Game
                {
                    Id = 1,
                    Name = "Queens",
                    Description = "LinkedIn Queens puzzle game",
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    ScoringType = ScoringType.Guesses
                },
                new Game
                {
                    Id = 2,
                    Name = "Pinpoint",
                    Description = "LinkedIn Pinpoint geography game",
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    ScoringType = ScoringType.Time
                },
                new Game
                {
                    Id = 3,
                    Name = "Crossclimb",
                    Description = "LinkedIn Crossclimb word ladder game",
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    IsActive = true,
                    ScoringType = ScoringType.Guesses
                }
            );
        }
    }
}
