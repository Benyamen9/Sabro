using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class AddBethGazoSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chants_identity_with_shuhlofo",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.DropIndex(
                name: "ix_chants_identity_without_shuhlofo",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.AlterColumn<Guid>(
                name: "mode_id",
                schema: "beth_gazo",
                table: "chants",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Backfilled to Farde, NOT to Guid.Empty as EF generated it. Empty is not
            // a real section, and section_id carries a foreign key with Restrict — so
            // the generated default would abort this migration on any existing chant
            // row. Production happens to hold none today, which makes the generated
            // version work by luck rather than by design; it would break the first
            // time the owner enters a chant before this deploys.
            //
            // Farde is the defensible backfill: every chant that predates this
            // migration has a non-null mode, so none of them can be a madrosho, and
            // the farde admit every mode. The FK is created further down, after the
            // sections are seeded, so the target row exists by the time it is checked.
            migrationBuilder.AddColumn<Guid>(
                name: "section_id",
                schema: "beth_gazo",
                table: "chants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("7a2c4b20-0000-4000-8000-000000000001"));

            migrationBuilder.CreateTable(
                name: "beth_gazo_sections",
                schema: "beth_gazo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beth_gazo_sections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "beth_gazo_section_modes",
                schema: "beth_gazo",
                columns: table => new
                {
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beth_gazo_section_modes", x => new { x.section_id, x.mode_id });
                    table.ForeignKey(
                        name: "fk_beth_gazo_section_modes_beth_gazo_modes_mode_id",
                        column: x => x.mode_id,
                        principalSchema: "beth_gazo",
                        principalTable: "beth_gazo_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_beth_gazo_section_modes_beth_gazo_sections_section_id",
                        column: x => x.section_id,
                        principalSchema: "beth_gazo",
                        principalTable: "beth_gazo_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                columns: new[] { "id", "created_at", "name", "position", "updated_at" },
                values: new object[,]
                {
                    { new Guid("7a2c4b20-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Farde", 1, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gnize", 2, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Madroshe", 3, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qonune yaunoye", 4, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tekso d-maurbe", 5, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mahebrone", 6, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                columns: new[] { "mode_id", "section_id" },
                values: new object[,]
                {
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000009"), new Guid("7a2c4b20-0000-4000-8000-000000000001") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000002") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000004") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000005") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000006") }
                });

            migrationBuilder.CreateIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "transliteration", "section_id", "mode_id", "shuhlofo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_chants_section_id",
                schema: "beth_gazo",
                table: "chants",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "ix_beth_gazo_section_modes_mode_id",
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                column: "mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_beth_gazo_sections_name",
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_beth_gazo_sections_position",
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                column: "position",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_chants_beth_gazo_sections_section_id",
                schema: "beth_gazo",
                table: "chants",
                column: "section_id",
                principalSchema: "beth_gazo",
                principalTable: "beth_gazo_sections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chants_beth_gazo_sections_section_id",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.DropTable(
                name: "beth_gazo_section_modes",
                schema: "beth_gazo");

            migrationBuilder.DropTable(
                name: "beth_gazo_sections",
                schema: "beth_gazo");

            migrationBuilder.DropIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.DropIndex(
                name: "ix_chants_section_id",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.DropColumn(
                name: "section_id",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.AlterColumn<Guid>(
                name: "mode_id",
                schema: "beth_gazo",
                table: "chants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chants_identity_with_shuhlofo",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "transliteration", "mode_id", "shuhlofo" },
                unique: true,
                filter: "shuhlofo IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chants_identity_without_shuhlofo",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "transliteration", "mode_id" },
                unique: true,
                filter: "shuhlofo IS NULL");
        }
    }
}
