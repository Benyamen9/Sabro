using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Play.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMnoDailyPuzzle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mno_daily_puzzles",
                schema: "play",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    expression = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tile_form = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mno_daily_puzzles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mno_daily_puzzles_game_id_date",
                schema: "play",
                table: "mno_daily_puzzles",
                columns: new[] { "game_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mno_daily_puzzles_game_id_expression",
                schema: "play",
                table: "mno_daily_puzzles",
                columns: new[] { "game_id", "expression" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mno_daily_puzzles",
                schema: "play");
        }
    }
}
