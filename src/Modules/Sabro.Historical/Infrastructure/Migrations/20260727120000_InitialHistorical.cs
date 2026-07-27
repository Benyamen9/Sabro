using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Historical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialHistorical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "historical");

            migrationBuilder.CreateTable(
                name: "historical_figures",
                schema: "historical",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    era = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    region = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tradition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    playable_in_shmo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historical_figures", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_historical_figures_name",
                schema: "historical",
                table: "historical_figures",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_historical_figures_status_playable_in_shmo",
                schema: "historical",
                table: "historical_figures",
                columns: new[] { "status", "playable_in_shmo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historical_figures",
                schema: "historical");
        }
    }
}
