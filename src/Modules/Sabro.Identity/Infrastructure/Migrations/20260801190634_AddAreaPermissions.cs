using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_area_permissions",
                schema: "identity",
                columns: table => new
                {
                    area = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_area_permissions", x => new { x.user_profile_id, x.area });
                    table.ForeignKey(
                        name: "fk_user_area_permissions_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalSchema: "identity",
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Carry existing access across. The four legacy area roles encoded area
            // and level in one column; each becomes a grant, and the role falls back
            // to Reader because it no longer answers that question.
            //
            // Owner, Reader and ExpertReviewer are deliberately untouched: Owner still
            // implies every area (RolePermissions says so in one place), and the other
            // two never granted area access to begin with.
            //
            // Without this, everyone holding an area role would silently lose their
            // access on deploy — the migration would look clean and the people would
            // simply find themselves locked out.
            migrationBuilder.Sql(@"
                INSERT INTO identity.user_area_permissions (user_profile_id, area, access)
                SELECT id, 'Lexicon', 'Reviewer' FROM identity.user_profiles WHERE role = 'LexiconReviewer'
                UNION ALL
                SELECT id, 'Lexicon', 'Editor'   FROM identity.user_profiles WHERE role = 'LexiconEditor'
                UNION ALL
                SELECT id, 'Shmo',    'Reviewer' FROM identity.user_profiles WHERE role = 'ShmoReviewer'
                UNION ALL
                SELECT id, 'Shmo',    'Editor'   FROM identity.user_profiles WHERE role = 'ShmoEditor'
                ON CONFLICT DO NOTHING;");

            migrationBuilder.Sql(@"
                UPDATE identity.user_profiles
                SET role = 'Reader'
                WHERE role IN ('LexiconReviewer', 'LexiconEditor', 'ShmoReviewer', 'ShmoEditor');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            // Best-effort reverse: fold single-area grants back into the role column.
            // Somebody holding two areas cannot be represented by one role, so the
            // wider grant wins and the narrower one is lost — which is exactly the
            // limitation this migration exists to remove. Rolling back is therefore
            // lossy by nature, not by oversight.
            migrationBuilder.Sql(@"
                UPDATE identity.user_profiles p
                SET role = CASE
                    WHEN g.area = 'Lexicon' AND g.access = 'Editor'   THEN 'LexiconEditor'
                    WHEN g.area = 'Lexicon' AND g.access = 'Reviewer' THEN 'LexiconReviewer'
                    WHEN g.area = 'Shmo'    AND g.access = 'Editor'   THEN 'ShmoEditor'
                    WHEN g.area = 'Shmo'    AND g.access = 'Reviewer' THEN 'ShmoReviewer'
                    ELSE p.role END
                FROM identity.user_area_permissions g
                WHERE g.user_profile_id = p.id AND p.role = 'Reader';");

            migrationBuilder.DropTable(
                name: "user_area_permissions",
                schema: "identity");
        }
    }
}
