using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Play.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMnoPuzzleDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mno_daily_puzzles_game_id_date",
                schema: "play",
                table: "mno_daily_puzzles");

            // Backfill "Normal": every puzzle served before the ladder existed
            // was the single (now Normal-labelled) daily.
            migrationBuilder.AddColumn<string>(
                name: "difficulty",
                schema: "play",
                table: "mno_daily_puzzles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.CreateIndex(
                name: "ix_mno_daily_puzzles_game_id_date_difficulty",
                schema: "play",
                table: "mno_daily_puzzles",
                columns: new[] { "game_id", "date", "difficulty" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_mno_daily_puzzles_game_id_date_difficulty",
                schema: "play",
                table: "mno_daily_puzzles");

            migrationBuilder.DropColumn(
                name: "difficulty",
                schema: "play",
                table: "mno_daily_puzzles");

            migrationBuilder.CreateIndex(
                name: "ix_mno_daily_puzzles_game_id_date",
                schema: "play",
                table: "mno_daily_puzzles",
                columns: new[] { "game_id", "date" },
                unique: true);
        }
    }
}
