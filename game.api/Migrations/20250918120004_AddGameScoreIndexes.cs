using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace game.api.Migrations
{
    /// <inheritdoc />
    public partial class AddGameScoreIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameScores_GameId",
                table: "GameScores");

            migrationBuilder.CreateIndex(
                name: "IX_GameScores_DateAchieved",
                table: "GameScores",
                column: "DateAchieved");

            migrationBuilder.CreateIndex(
                name: "IX_GameScores_GameId_DateAchieved",
                table: "GameScores",
                columns: new[] { "GameId", "DateAchieved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameScores_DateAchieved",
                table: "GameScores");

            migrationBuilder.DropIndex(
                name: "IX_GameScores_GameId_DateAchieved",
                table: "GameScores");

            migrationBuilder.CreateIndex(
                name: "IX_GameScores_GameId",
                table: "GameScores",
                column: "GameId");
        }
    }
}
