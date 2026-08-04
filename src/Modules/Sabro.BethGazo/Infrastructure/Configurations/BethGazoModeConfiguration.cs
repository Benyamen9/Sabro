using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Infrastructure.Configurations;

internal sealed class BethGazoModeConfiguration : IEntityTypeConfiguration<BethGazoMode>
{
    public void Configure(EntityTypeBuilder<BethGazoMode> builder)
    {
        builder.ToTable("BethGazoModes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(BethGazoMode.MaxNameLength).IsRequired();
        builder.Property(e => e.Position).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Name).IsUnique();

        // Position is unique too — two modes sharing a slot would make the
        // traditional ordering ambiguous. Deliberately NOT capped at eight: the
        // owner has more than eight in some sets.
        builder.HasIndex(e => e.Position).IsUnique();

        Seed(builder);
    }

    /// <summary>
    /// The eight modes of the Beth Gazo, owner-confirmed 2026-08-04. Seeded because
    /// the table is a foreign key target: without rows, no chant can be created at
    /// all, and the first editor would have to invent them before doing any work.
    /// </summary>
    /// <remarks>
    /// These are a starting point, not the closed set — the owner has sets with more
    /// than eight, and adding one is a row rather than a deploy. Ids and timestamps
    /// are fixed literals because <c>HasData</c> is compared against the model on
    /// every <c>migrations add</c>: generated values would produce a spurious
    /// migration each time.
    /// </remarks>
    private static void Seed(EntityTypeBuilder<BethGazoMode> builder)
    {
        var seededAt = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero);

        var modes = new (string Id, string Name, int Position)[]
        {
            ("6f9b1a10-0000-4000-8000-000000000001", "Qadmoyo", 1),
            ("6f9b1a10-0000-4000-8000-000000000002", "Trayono", 2),
            ("6f9b1a10-0000-4000-8000-000000000003", "Tlithoyo", 3),
            ("6f9b1a10-0000-4000-8000-000000000004", "Rbiʿoyo", 4),
            ("6f9b1a10-0000-4000-8000-000000000005", "Hmishoyo", 5),
            ("6f9b1a10-0000-4000-8000-000000000006", "Shtithoyo", 6),
            ("6f9b1a10-0000-4000-8000-000000000007", "Shbiʿoyo", 7),
            ("6f9b1a10-0000-4000-8000-000000000008", "Tminoyo", 8),
        };

        builder.HasData(modes.Select(mode => new
        {
            Id = Guid.Parse(mode.Id),
            mode.Name,
            mode.Position,
            CreatedAt = seededAt,
            UpdatedAt = seededAt,
        }));
    }
}
