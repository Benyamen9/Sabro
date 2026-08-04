using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class InitialBethGazo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "beth_gazo");

            migrationBuilder.CreateTable(
                name: "beth_gazo_modes",
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
                    table.PrimaryKey("pk_beth_gazo_modes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chants",
                schema: "beth_gazo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    syriac_incipit = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    syriac_incipit_vocalized = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    transliteration = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    mode_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shuhlofo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    inherits_melody_from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audio_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    playable_in_nahlo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chants", x => x.id);
                    table.ForeignKey(
                        name: "fk_chants_beth_gazo_modes_mode_id",
                        column: x => x.mode_id,
                        principalSchema: "beth_gazo",
                        principalTable: "beth_gazo_modes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_chants_chants_inherits_melody_from_id",
                        column: x => x.inherits_melody_from_id,
                        principalSchema: "beth_gazo",
                        principalTable: "chants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                columns: new[] { "id", "created_at", "name", "position", "updated_at" },
                values: new object[,]
                {
                    { new Guid("6f9b1a10-0000-4000-8000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Qadmoyo", 1, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Trayono", 2, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tlithoyo", 3, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rbiʿoyo", 4, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hmishoyo", 5, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000006"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Shtithoyo", 6, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000007"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Shbiʿoyo", 7, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("6f9b1a10-0000-4000-8000-000000000008"), new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tminoyo", 8, new DateTimeOffset(new DateTime(2026, 8, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_beth_gazo_modes_name",
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_beth_gazo_modes_position",
                schema: "beth_gazo",
                table: "beth_gazo_modes",
                column: "position",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "ix_chants_inherits_melody_from_id",
                schema: "beth_gazo",
                table: "chants",
                column: "inherits_melody_from_id");

            migrationBuilder.CreateIndex(
                name: "ix_chants_mode_id",
                schema: "beth_gazo",
                table: "chants",
                column: "mode_id");

            migrationBuilder.CreateIndex(
                name: "ix_chants_status_playable_in_nahlo",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "status", "playable_in_nahlo" });

            migrationBuilder.CreateIndex(
                name: "ix_chants_transliteration",
                schema: "beth_gazo",
                table: "chants",
                column: "transliteration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chants",
                schema: "beth_gazo");

            migrationBuilder.DropTable(
                name: "beth_gazo_modes",
                schema: "beth_gazo");
        }
    }
}
