using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Historical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalFigureDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historical_figure_descriptions",
                schema: "historical",
                columns: table => new
                {
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    historical_figure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historical_figure_descriptions", x => new { x.historical_figure_id, x.language });
                    table.ForeignKey(
                        name: "fk_historical_figure_descriptions_historical_figures_historica~",
                        column: x => x.historical_figure_id,
                        principalSchema: "historical",
                        principalTable: "historical_figures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historical_figure_descriptions",
                schema: "historical");
        }
    }
}
