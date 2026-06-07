using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Play.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "play");

            migrationBuilder.CreateTable(
                name: "game_results",
                schema: "play",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logto_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    game_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    played_on = table.Column<DateOnly>(type: "date", nullable: false),
                    solved = table.Column<bool>(type: "boolean", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    detail_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meltha_daily_puzzles",
                schema: "play",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    lexicon_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meltha_daily_puzzles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_results_logto_user_id_game_id_played_on",
                schema: "play",
                table: "game_results",
                columns: new[] { "logto_user_id", "game_id", "played_on" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meltha_daily_puzzles_game_id_date",
                schema: "play",
                table: "meltha_daily_puzzles",
                columns: new[] { "game_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_meltha_daily_puzzles_game_id_lexicon_entry_id",
                schema: "play",
                table: "meltha_daily_puzzles",
                columns: new[] { "game_id", "lexicon_entry_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_results",
                schema: "play");

            migrationBuilder.DropTable(
                name: "meltha_daily_puzzles",
                schema: "play");
        }
    }
}
