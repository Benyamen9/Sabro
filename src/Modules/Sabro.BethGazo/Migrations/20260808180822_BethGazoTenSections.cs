using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class BethGazoTenSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Removes Mahebrone (section …0006) and its eight mode links, at the
            // owner's instruction 2026-08-08: it is a real liturgical genre but not
            // one of the ten sections Ibrahim & Kiraz enumerate for the abridged
            // Beth Gazo. Restoring it is one seed row plus its links.
            //
            // Safe because chants.section_id carries a Restrict foreign key and the
            // table holds no rows: a chant filed under Mahebrone would abort this
            // migration loudly rather than be orphaned, which is the behaviour we
            // want if this is ever re-run against a populated database.
            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000006"));

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                columns: new[] { "id", "created_at", "name", "position", "updated_at" },
                values: new object[,]
                {
                    { new Guid("7a2c4b20-0000-4000-8000-000000000008"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Gushmo", 8, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-000000000009"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Takheshphotho rabuloyotho", 9, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-00000000000a"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tborto", 10, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("7a2c4b20-0000-4000-8000-00000000000b"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Quqlion", 11, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                columns: new[] { "mode_id", "section_id" },
                values: new object[,]
                {
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000008") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000009") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000008") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000009") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-00000000000a") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                keyColumns: new[] { "mode_id", "section_id" },
                keyValues: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-00000000000b") });

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-00000000000a"));

            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                keyColumn: "id",
                keyValue: new Guid("7a2c4b20-0000-4000-8000-00000000000b"));

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_sections",
                columns: new[] { "id", "created_at", "name", "position", "updated_at" },
                values: new object[] { new Guid("7a2c4b20-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mahebrone", 6, new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_section_modes",
                columns: new[] { "mode_id", "section_id" },
                values: new object[,]
                {
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new Guid("7a2c4b20-0000-4000-8000-000000000006") },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new Guid("7a2c4b20-0000-4000-8000-000000000006") }
                });
        }
    }
}
