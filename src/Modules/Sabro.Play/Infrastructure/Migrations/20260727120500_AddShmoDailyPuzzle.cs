using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Play.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShmoDailyPuzzle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shmo_daily_puzzles",
                schema: "play",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    historical_figure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shmo_daily_puzzles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shmo_daily_puzzles_game_id_date",
                schema: "play",
                table: "shmo_daily_puzzles",
                columns: new[] { "game_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shmo_daily_puzzles_game_id_historical_figure_id",
                schema: "play",
                table: "shmo_daily_puzzles",
                columns: new[] { "game_id", "historical_figure_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shmo_daily_puzzles",
                schema: "play");
        }
    }
}
