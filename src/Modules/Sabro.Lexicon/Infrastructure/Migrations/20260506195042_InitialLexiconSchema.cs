using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sabro.Lexicon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLexiconSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lexicon");

            migrationBuilder.CreateTable(
                name: "lexicon_roots",
                schema: "lexicon",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lexicon_roots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lexicon_entries",
                schema: "lexicon",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    syriac_unvocalized = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    syriac_vocalized = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    root_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sbl_transliteration = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    grammatical_category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    morphology = table.Column<string>(type: "text", nullable: true),
                    transliteration_variants = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lexicon_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_lexicon_entries_lexicon_roots_root_id",
                        column: x => x.root_id,
                        principalSchema: "lexicon",
                        principalTable: "lexicon_roots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "lexicon_entry_meanings",
                schema: "lexicon",
                columns: table => new
                {
                    lexicon_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lexicon_entry_meanings", x => new { x.lexicon_entry_id, x.position });
                    table.ForeignKey(
                        name: "fk_lexicon_entry_meanings_lexicon_entries_lexicon_entry_id",
                        column: x => x.lexicon_entry_id,
                        principalSchema: "lexicon",
                        principalTable: "lexicon_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_lexicon_entries_root_id",
                schema: "lexicon",
                table: "lexicon_entries",
                column: "root_id");

            migrationBuilder.CreateIndex(
                name: "ix_lexicon_entries_sbl_transliteration",
                schema: "lexicon",
                table: "lexicon_entries",
                column: "sbl_transliteration");

            migrationBuilder.CreateIndex(
                name: "ix_lexicon_entries_syriac_unvocalized",
                schema: "lexicon",
                table: "lexicon_entries",
                column: "syriac_unvocalized");

            migrationBuilder.CreateIndex(
                name: "ix_lexicon_roots_form",
                schema: "lexicon",
                table: "lexicon_roots",
                column: "form",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lexicon_entry_meanings",
                schema: "lexicon");

            migrationBuilder.DropTable(
                name: "lexicon_entries",
                schema: "lexicon");

            migrationBuilder.DropTable(
                name: "lexicon_roots",
                schema: "lexicon");
        }
    }
}
