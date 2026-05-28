using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Biblical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCrossReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cross_references",
                schema: "biblical",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    annotation_anchor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    passage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cross_references", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cross_references_annotation_anchor_id",
                schema: "biblical",
                table: "cross_references",
                column: "annotation_anchor_id");

            migrationBuilder.CreateIndex(
                name: "ix_cross_references_annotation_anchor_id_passage_id_source_kind",
                schema: "biblical",
                table: "cross_references",
                columns: new[] { "annotation_anchor_id", "passage_id", "source", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cross_references_passage_id",
                schema: "biblical",
                table: "cross_references",
                column: "passage_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cross_references",
                schema: "biblical");
        }
    }
}
