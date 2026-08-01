using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Reviews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "target_version",
                schema: "reviews",
                table: "suggested_edits",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "accepted_despite_change",
                schema: "reviews",
                table: "suggested_edits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "field",
                schema: "reviews",
                table: "suggested_edits",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_value",
                schema: "reviews",
                table: "suggested_edits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "target_updated_at",
                schema: "reviews",
                table: "suggested_edits",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accepted_despite_change",
                schema: "reviews",
                table: "suggested_edits");

            migrationBuilder.DropColumn(
                name: "field",
                schema: "reviews",
                table: "suggested_edits");

            migrationBuilder.DropColumn(
                name: "original_value",
                schema: "reviews",
                table: "suggested_edits");

            migrationBuilder.DropColumn(
                name: "target_updated_at",
                schema: "reviews",
                table: "suggested_edits");

            migrationBuilder.AlterColumn<int>(
                name: "target_version",
                schema: "reviews",
                table: "suggested_edits",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
