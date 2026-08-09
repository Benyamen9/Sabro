using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class AddMshahelfothoMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                columns: new[] { "id", "created_at", "name", "position", "updated_at" },
                values: new object[] { new Guid("6f9b1a10-0000-4000-8000-000000000009"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mshaḥelfotho", 9, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                keyColumn: "id",
                keyValue: new Guid("6f9b1a10-0000-4000-8000-000000000009"));
        }
    }
}
