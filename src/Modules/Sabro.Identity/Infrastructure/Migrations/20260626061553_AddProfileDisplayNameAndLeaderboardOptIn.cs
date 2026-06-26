using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileDisplayNameAndLeaderboardOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_leaderboard",
                schema: "identity",
                table: "user_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "show_on_leaderboard",
                schema: "identity",
                table: "user_profiles");
        }
    }
}
