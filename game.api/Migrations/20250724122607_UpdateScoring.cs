using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace game.api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "GameScores");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "CompletionTime",
                table: "GameScores",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuessCount",
                table: "GameScores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoringType",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 1,
                column: "ScoringType",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 2,
                column: "ScoringType",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 3,
                column: "ScoringType",
                value: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionTime",
                table: "GameScores");

            migrationBuilder.DropColumn(
                name: "GuessCount",
                table: "GameScores");

            migrationBuilder.DropColumn(
                name: "ScoringType",
                table: "Games");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "GameScores",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
