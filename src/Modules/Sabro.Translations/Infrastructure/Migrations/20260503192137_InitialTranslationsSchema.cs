using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Translations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTranslationsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "translations");

            migrationBuilder.CreateTable(
                name: "authors",
                schema: "translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    syriac_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "text_versions",
                schema: "translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_right_to_left = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_text_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sources",
                schema: "translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    original_language_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_sources_authors_author_id",
                        column: x => x.author_id,
                        principalSchema: "translations",
                        principalTable: "authors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "segments",
                schema: "translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_number = table.Column<int>(type: "integer", nullable: false),
                    verse_number = table.Column<int>(type: "integer", nullable: false),
                    text_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    previous_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_segments", x => x.id);
                    table.ForeignKey(
                        name: "fk_segments_segments_previous_version_id",
                        column: x => x.previous_version_id,
                        principalSchema: "translations",
                        principalTable: "segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_segments_sources_source_id",
                        column: x => x.source_id,
                        principalSchema: "translations",
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_segments_text_versions_text_version_id",
                        column: x => x.text_version_id,
                        principalSchema: "translations",
                        principalTable: "text_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "annotations",
                schema: "translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    anchor_start = table.Column<int>(type: "integer", nullable: false),
                    anchor_end = table.Column<int>(type: "integer", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_annotations", x => x.id);
                    table.ForeignKey(
                        name: "fk_annotations_segments_segment_id",
                        column: x => x.segment_id,
                        principalSchema: "translations",
                        principalTable: "segments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_annotations_segment_id",
                schema: "translations",
                table: "annotations",
                column: "segment_id");

            migrationBuilder.CreateIndex(
                name: "ix_segments_previous_version_id",
                schema: "translations",
                table: "segments",
                column: "previous_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_segments_source_id_chapter_number_verse_number_text_version~",
                schema: "translations",
                table: "segments",
                columns: new[] { "source_id", "chapter_number", "verse_number", "text_version_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_segments_text_version_id",
                schema: "translations",
                table: "segments",
                column: "text_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_sources_author_id",
                schema: "translations",
                table: "sources",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_text_versions_code",
                schema: "translations",
                table: "text_versions",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annotations",
                schema: "translations");

            migrationBuilder.DropTable(
                name: "segments",
                schema: "translations");

            migrationBuilder.DropTable(
                name: "sources",
                schema: "translations");

            migrationBuilder.DropTable(
                name: "text_versions",
                schema: "translations");

            migrationBuilder.DropTable(
                name: "authors",
                schema: "translations");
        }
    }
}
