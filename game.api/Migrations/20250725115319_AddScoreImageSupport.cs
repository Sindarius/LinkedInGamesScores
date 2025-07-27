using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace game.api.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreImageSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "GameScores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ScoreImage",
                table: "GameScores",
                type: "bytea",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "GameScores");

            migrationBuilder.DropColumn(
                name: "ScoreImage",
                table: "GameScores");
        }
    }
}
