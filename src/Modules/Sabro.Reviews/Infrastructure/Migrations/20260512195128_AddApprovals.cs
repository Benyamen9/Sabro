using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Reviews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "approvals",
                schema: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chapter_number = table.Column<int>(type: "integer", nullable: false),
                    verse_number = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    decision_by_logto_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    decision_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approvals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_approvals_decision_by_logto_user_id",
                schema: "reviews",
                table: "approvals",
                column: "decision_by_logto_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_source_id_chapter_number_target_type",
                schema: "reviews",
                table: "approvals",
                columns: new[] { "source_id", "chapter_number", "target_type" });

            migrationBuilder.CreateIndex(
                name: "ix_approvals_source_id_chapter_number_verse_number",
                schema: "reviews",
                table: "approvals",
                columns: new[] { "source_id", "chapter_number", "verse_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approvals",
                schema: "reviews");
        }
    }
}
