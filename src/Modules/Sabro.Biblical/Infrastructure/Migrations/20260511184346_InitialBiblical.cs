using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Biblical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBiblical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "biblical");

            migrationBuilder.CreateTable(
                name: "biblical_books",
                schema: "biblical",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    english_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    syriac_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    testament = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_biblical_books", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "biblical_passages",
                schema: "biblical",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_number = table.Column<int>(type: "integer", nullable: false),
                    verse_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_biblical_passages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_biblical_books_code",
                schema: "biblical",
                table: "biblical_books",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_biblical_passages_book_id_chapter_number_verse_number",
                schema: "biblical",
                table: "biblical_passages",
                columns: new[] { "book_id", "chapter_number", "verse_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "biblical_books",
                schema: "biblical");

            migrationBuilder.DropTable(
                name: "biblical_passages",
                schema: "biblical");
        }
    }
}
