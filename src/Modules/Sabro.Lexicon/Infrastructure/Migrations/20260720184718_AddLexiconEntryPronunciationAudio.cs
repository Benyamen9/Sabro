using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Lexicon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLexiconEntryPronunciationAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pronunciation_audio_url",
                schema: "lexicon",
                table: "lexicon_entries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pronunciation_audio_url",
                schema: "lexicon",
                table: "lexicon_entries");
        }
    }
}
