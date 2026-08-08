using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class AddQoleShahroyeSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                columns: new[] { "id", "created_at", "name", "position", "updated_at" },
                values: new object[] { new Guid("7a2c4b20-0000-4000-8000-000000000007"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qole shahroye", 7, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                columns: new[] { "mode_id", "section_id" },
                values: new object[,]
                {
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000007") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000007") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000007") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000007") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000007") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000007") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000007") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000007") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000007"));
        }
    }
}
