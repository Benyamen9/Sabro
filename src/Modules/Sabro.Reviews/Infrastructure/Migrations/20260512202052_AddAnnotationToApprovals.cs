using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Reviews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnotationToApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "annotation_id",
                schema: "reviews",
                table: "approvals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_approvals_annotation_id",
                schema: "reviews",
                table: "approvals",
                column: "annotation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_approvals_annotation_id",
                schema: "reviews",
                table: "approvals");

            migrationBuilder.DropColumn(
                name: "annotation_id",
                schema: "reviews",
                table: "approvals");
        }
    }
}
