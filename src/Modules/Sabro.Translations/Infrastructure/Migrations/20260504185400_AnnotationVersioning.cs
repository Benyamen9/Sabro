using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Translations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnnotationVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "previous_version_id",
                schema: "translations",
                table: "annotations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                schema: "translations",
                table: "annotations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_annotations_previous_version_id",
                schema: "translations",
                table: "annotations",
                column: "previous_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_annotations_segment_id_anchor_start_anchor_end_version",
                schema: "translations",
                table: "annotations",
                columns: new[] { "segment_id", "anchor_start", "anchor_end", "version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_annotations_annotations_previous_version_id",
                schema: "translations",
                table: "annotations",
                column: "previous_version_id",
                principalSchema: "translations",
                principalTable: "annotations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_annotations_annotations_previous_version_id",
                schema: "translations",
                table: "annotations");

            migrationBuilder.DropIndex(
                name: "ix_annotations_previous_version_id",
                schema: "translations",
                table: "annotations");

            migrationBuilder.DropIndex(
                name: "ix_annotations_segment_id_anchor_start_anchor_end_version",
                schema: "translations",
                table: "annotations");

            migrationBuilder.DropColumn(
                name: "previous_version_id",
                schema: "translations",
                table: "annotations");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "translations",
                table: "annotations");
        }
    }
}
