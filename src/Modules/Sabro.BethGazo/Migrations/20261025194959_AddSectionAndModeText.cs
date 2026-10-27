using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionAndModeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "syriac_name",
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "syriac_name",
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "beth_gazo_mode_descriptions",
                schema: "beth_gazo",
                columns: table => new
                {
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    beth_gazo_mode_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beth_gazo_mode_descriptions", x => new { x.beth_gazo_mode_id, x.language });
                    table.ForeignKey(
                        name: "fk_beth_gazo_mode_descriptions_beth_gazo_modes_beth_gazo_mode_~",
                        column: x => x.beth_gazo_mode_id,
                        principalSchema: "beth_gazo",
                        principalTable: "beth_gazo_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "beth_gazo_section_descriptions",
                schema: "beth_gazo",
                columns: table => new
                {
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    beth_gazo_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_beth_gazo_section_descriptions", x => new { x.beth_gazo_section_id, x.language });
                    table.ForeignKey(
                        name: "fk_beth_gazo_section_descriptions_beth_gazo_sections_beth_gazo~",
                        column: x => x.beth_gazo_section_id,
                        principalSchema: "beth_gazo",
                        principalTable: "beth_gazo_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000001"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000002"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000003"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000004"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000005"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000006"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000007"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000008"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000009"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000001"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000002"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000003"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000004"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000005"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000007"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000008"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000009"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-00000000000a"),
                column: "syriac_name",
                value: null);

            migrationBuilder.UpdateData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-00000000000b"),
                column: "syriac_name",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beth_gazo_mode_descriptions",
                schema: "beth_gazo");

            migrationBuilder.DropTable(
                name: "beth_gazo_section_descriptions",
                schema: "beth_gazo");

            migrationBuilder.DropColumn(
                name: "syriac_name",
                schema: "beth_gazo",
                table: "beth_gazo_sections");

            migrationBuilder.DropColumn(
                name: "syriac_name",
                schema: "beth_gazo",
                table: "beth_gazo_modes");
        }
    }
}
