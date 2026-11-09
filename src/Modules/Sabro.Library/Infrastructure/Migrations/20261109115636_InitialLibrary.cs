using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Library.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "library");

            migrationBuilder.CreateTable(
                name: "reading_marks",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logto_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    book = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    item_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    marked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reading_marks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reading_places",
                schema: "library",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logto_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    book = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    item_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reading_places", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reading_marks_logto_user_id_book_item_key",
                schema: "library",
                table: "reading_marks",
                columns: new[] { "logto_user_id", "book", "item_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reading_places_logto_user_id_book",
                schema: "library",
                table: "reading_places",
                columns: new[] { "logto_user_id", "book" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reading_marks",
                schema: "library");

            migrationBuilder.DropTable(
                name: "reading_places",
                schema: "library");
        }
    }
}
