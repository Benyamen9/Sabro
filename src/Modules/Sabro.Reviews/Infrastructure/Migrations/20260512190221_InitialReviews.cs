using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Reviews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reviews");

            migrationBuilder.CreateTable(
                name: "suggested_edits",
                schema: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_version = table.Column<int>(type: "integer", nullable: false),
                    proposed_content = table.Column<string>(type: "text", nullable: false),
                    rationale = table.Column<string>(type: "text", nullable: true),
                    submitted_by_logto_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    decision_by_logto_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    decision_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suggested_edits", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_suggested_edits_submitted_by_logto_user_id",
                schema: "reviews",
                table: "suggested_edits",
                column: "submitted_by_logto_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_suggested_edits_target_type_target_id_status",
                schema: "reviews",
                table: "suggested_edits",
                columns: new[] { "target_type", "target_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suggested_edits",
                schema: "reviews");
        }
    }
}
