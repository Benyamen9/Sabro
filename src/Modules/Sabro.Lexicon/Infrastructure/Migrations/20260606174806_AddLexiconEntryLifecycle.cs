using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Lexicon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLexiconEntryLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "sbl_transliteration",
                schema: "lexicon",
                table: "lexicon_entries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<bool>(
                name: "playable_in_meltha",
                schema: "lexicon",
                table: "lexicon_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "playable_length",
                schema: "lexicon",
                table: "lexicon_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "lexicon",
                table: "lexicon_entries",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateIndex(
                name: "ix_lexicon_entries_status_playable_in_meltha_playable_length",
                schema: "lexicon",
                table: "lexicon_entries",
                columns: new[] { "status", "playable_in_meltha", "playable_length" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_lexicon_entries_status_playable_in_meltha_playable_length",
                schema: "lexicon",
                table: "lexicon_entries");

            migrationBuilder.DropColumn(
                name: "playable_in_meltha",
                schema: "lexicon",
                table: "lexicon_entries");

            migrationBuilder.DropColumn(
                name: "playable_length",
                schema: "lexicon",
                table: "lexicon_entries");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "lexicon",
                table: "lexicon_entries");

            migrationBuilder.AlterColumn<string>(
                name: "sbl_transliteration",
                schema: "lexicon",
                table: "lexicon_entries",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
