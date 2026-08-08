using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class ShuhlofoBecomesANumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ This DROPS a column, which the project's migration rule forbids in
            // a single deploy: destructive changes are supposed to go expand →
            // migrate → contract across several. The exception is deliberate and
            // narrow — `beth_gazo.chants` holds ZERO rows in production (the pool is
            // empty, which is why /play/nahlo/today still answers a clean 409), and
            // Nahlo is not deployed, so nothing reads the old shape either. There is
            // no data to preserve and no client to break.
            //
            // Do NOT take this as precedent once chants exist. The same change then
            // needs a new column, a backfill, and a later contract.
            migrationBuilder.DropIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.DropColumn(
                name: "shuhlofo",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.AddColumn<int>(
                name: "shuhlofo_number",
                schema: "beth_gazo",
                table: "chants",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "transliteration", "section_id", "mode_id", "shuhlofo_number" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.DropColumn(
                name: "shuhlofo_number",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.AddColumn<string>(
                name: "shuhlofo",
                schema: "beth_gazo",
                table: "chants",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "transliteration", "section_id", "mode_id", "shuhlofo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
