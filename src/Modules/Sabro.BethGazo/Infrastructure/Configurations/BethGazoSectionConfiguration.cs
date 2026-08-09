using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Infrastructure.Configurations;

internal sealed class BethGazoSectionConfiguration : IEntityTypeConfiguration<BethGazoSection>
{
    /// <summary>
    /// Fixed ids for the seeded sections. Literals rather than generated values,
    /// for the same reason the modes' are: <c>HasData</c> is diffed against the
    /// model on every <c>migrations add</c>, so a generated id would produce a
    /// spurious migration every time.
    /// </summary>
    internal const string FardeId = "7a2c4b20-0000-4000-8000-000000000001";
    internal const string MadrosheId = "7a2c4b20-0000-4000-8000-000000000003";
    internal const string QoleShahroyeId = "7a2c4b20-0000-4000-8000-000000000007";
    internal const string GushmoId = "7a2c4b20-0000-4000-8000-000000000008";
    internal const string TakhshefthoId = "7a2c4b20-0000-4000-8000-000000000009";
    internal const string TbortoId = "7a2c4b20-0000-4000-8000-00000000000a";
    internal const string QuqlionId = "7a2c4b20-0000-4000-8000-00000000000b";

    public void Configure(EntityTypeBuilder<BethGazoSection> builder)
    {
        builder.ToTable("BethGazoSections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(BethGazoSection.MaxNameLength).IsRequired();
        builder.Property(e => e.Position).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.Position).IsUnique();

        // The allowed-mode links, owned by the section: they have no life of their
        // own and are always read through it.
        builder.HasMany(e => e.AllowedModes)
            .WithOne()
            .HasForeignKey(m => m.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.AllowedModes)
            .HasField("allowedModes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(e => e.AllowedModeIds);
        builder.Ignore(e => e.HasModes);

        Seed(builder);
    }

    /// <summary>
    /// The sections of the treasury the owner has named so far.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Mahebrone was removed on 2026-08-08</b> at the owner's instruction. It is a real
    /// liturgical genre (maʿbrone, funeral songs) and appears in the Patriarchate's wider
    /// 24-category listing, but it is <b>not one of the ten sections of the abridged Beth
    /// Gazo</b>. Restoring it is one row here plus its mode links.
    /// </para>
    /// <para>
    /// <b>This list is his, and it is incomplete.</b> He named these on 2026-08-07
    /// and 2026-08-08 and ended the list with "and more", so treat it as a starting
    /// point an editor extends — never as the closed set. The spellings are his own
    /// rather than verified SBL, and they are the labels a player will read, so they
    /// want his correction the same way the qfiso transliterations do. (His own
    /// spelling wavers too — "Madrosche" once, "madroshe" elsewhere.)
    /// </para>
    /// <para>
    /// Seeded because the table is a foreign key target and every chant needs one:
    /// without rows, no chant can be created at all.
    /// </para>
    /// </remarks>
    private static void Seed(EntityTypeBuilder<BethGazoSection> builder)
    {
        var seededAt = new DateTimeOffset(2026, 8, 8, 0, 0, 0, TimeSpan.Zero);

        var sections = new (string Id, string Name, int Position)[]
        {
            (FardeId, "Farde", 1),
            ("7a2c4b20-0000-4000-8000-000000000002", "Gnize", 2),
            (MadrosheId, "Madroshe", 3),
            ("7a2c4b20-0000-4000-8000-000000000004", "Qonune yaunoye", 4),
            ("7a2c4b20-0000-4000-8000-000000000005", "Tekso d-maurbe", 5),

            // Owner, 2026-08-08: "zodeq dnehwe, ... are qole shahroyo" — the
            // section every worked example in this module belongs to, and the one
            // the first seed missed entirely. Appended at 7 rather than inserted
            // in the treasury's real order: Position is a sort key only, so
            // renumbering later is free and never touches a chant's link.
            (QoleShahroyeId, "Qole shahroye", 7),

            // Completing the ten sections Ibrahim & Kiraz enumerate for the
            // abridged Beth Gazo (Dolabani 1913, the qfiso). Each is given eight
            // modes by that source in as many words.
            (GushmoId, "Gushmo", 8),
            (TakhshefthoId, "Takheshphotho rabuloyotho", 9),
            (TbortoId, "Tborto", 10),
            (QuqlionId, "Quqlion", 11),
        };

        builder.HasData(sections.Select(section => new
        {
            Id = Guid.Parse(section.Id),
            section.Name,
            section.Position,
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
        }));
    }
}
