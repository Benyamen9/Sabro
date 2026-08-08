using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Infrastructure.Configurations;

internal sealed class BethGazoSectionModeConfiguration : IEntityTypeConfiguration<BethGazoSectionMode>
{
    /// <summary>The eight ordinals. Their ids are fixed literals in <see cref="BethGazoModeConfiguration"/>.</summary>
    private const string ModeIdPrefix = "6f9b1a10-0000-4000-8000-00000000000";

    public void Configure(EntityTypeBuilder<BethGazoSectionMode> builder)
    {
        builder.ToTable("BethGazoSectionModes");

        // The pair is the identity — a section admits a mode once or not at all.
        builder.HasKey(e => new { e.SectionId, e.ModeId });

        builder.HasOne<BethGazoMode>()
            .WithMany()
            .HasForeignKey(e => e.ModeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ModeId);

        Seed(builder);
    }

    /// <summary>
    /// Which modes each section admits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two rows of this table are the owner's own rules, stated 2026-08-08:</b>
    /// the farde admit all nine (the eight ordinals plus the mshaḥelfotho, which is
    /// theirs alone), and the madroshe admit none — which is why the madroshe appear
    /// nowhere below. An absent section is a mode-less section, and that is the
    /// whole mechanism.
    /// </para>
    /// <para>
    /// ⚠️ <b>The other four sections carry the eight as an assumption, not as his
    /// instruction.</b> He told us only the two rules above. Giving gnize, qonune
    /// yaunoye, tekso d-maurbe and mahebrone the eight ordinals follows the
    /// tradition's ordinary shape, but it has not been confirmed — and it is the
    /// kind of claim that becomes a wrong answer in a game where every attribute is
    /// the answer. Both failure directions are real: a section wrongly given the
    /// eight asks a question with no right answer, and a section wrongly left empty
    /// never asks one that mattered. Correct these in the backoffice rather than
    /// assuming the seed is authoritative.
    /// </para>
    /// </remarks>
    private static void Seed(EntityTypeBuilder<BethGazoSectionMode> builder)
    {
        var eightOrdinals = Enumerable.Range(1, 8)
            .Select(n => Guid.Parse($"{ModeIdPrefix}{n}"))
            .ToArray();

        var mshahelfotho = Guid.Parse($"{ModeIdPrefix}9");

        var links = new List<object>();

        // Farde — the eight, plus the mshaḥelfotho that belongs to them alone.
        foreach (var modeId in eightOrdinals.Append(mshahelfotho))
        {
            links.Add(new { SectionId = Guid.Parse(BethGazoSectionConfiguration.FardeId), ModeId = modeId });
        }

        // Madroshe are deliberately absent: no rows means no mode.

        // The remaining sections — assumed, see the remarks above.
        var assumedEight = new[]
        {
            "7a2c4b20-0000-4000-8000-000000000002", // Gnize
            "7a2c4b20-0000-4000-8000-000000000004", // Qonune yaunoye
            "7a2c4b20-0000-4000-8000-000000000005", // Tekso d-maurbe
            "7a2c4b20-0000-4000-8000-000000000006", // Mahebrone
        };

        foreach (var sectionId in assumedEight)
        {
            foreach (var modeId in eightOrdinals)
            {
                links.Add(new { SectionId = Guid.Parse(sectionId), ModeId = modeId });
            }
        }

        builder.HasData(links);
    }
}
