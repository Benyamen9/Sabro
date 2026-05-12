using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Reader");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                schema: "identity",
                table: "user_profiles");
        }
    }
}
