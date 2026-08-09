using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sabro.BethGazo.Migrations
{
    /// <inheritdoc />
    public partial class ChantVariantKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ⚠️ Renames a column and rebuilds the identity index, which the
            // project's migration rule forbids in a single deploy — destructive
            // changes are meant to go expand → migrate → contract. The exception is
            // the same narrow one as ShuhlofoBecomesANumber: beth_gazo.chants holds
            // ZERO rows in production and Nahlo is not deployed, so there is no data
            // to preserve and no client reading the old shape.
            //
            // The identity gains variant_kind because a shuḥlofo 1 and a ḥrino 1
            // under one melody and mode are different chants: without the kind in
            // the key they collide and the second cannot be saved at all.
            migrationBuilder.DropIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.RenameColumn(
                name: "shuhlofo_number",
                schema: "beth_gazo",
                table: "chants",
                newName: "variant_number");

            migrationBuilder.AddColumn<string>(
                name: "variant_kind",
                schema: "beth_gazo",
                table: "chants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                // "None", not the empty string EF generated. The column is a
                // string-converted enum: "" is not a member and would fail to
                // parse back on read. Harmless today because the table is empty,
                // exactly like the Guid.Empty default this repeats — but wrong,
                // and wrong in a way that only shows up once there are rows.
                defaultValue: "None");

            migrationBuilder.CreateIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "transliteration", "section_id", "mode_id", "variant_kind", "variant_number" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.DropColumn(
                name: "variant_kind",
                schema: "beth_gazo",
                table: "chants");

            migrationBuilder.RenameColumn(
                name: "variant_number",
                schema: "beth_gazo",
                table: "chants",
                newName: "shuhlofo_number");

            migrationBuilder.CreateIndex(
                name: "ix_chants_identity",
                schema: "beth_gazo",
                table: "chants",
                columns: new[] { "transliteration", "section_id", "mode_id", "shuhlofo_number" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
