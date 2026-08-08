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
    /// <b>The other four are owner-confirmed too, 2026-08-08:</b> "yaunoye 8. Maurbe
    /// 8. Gnize 8, multiple chants for 7. mahebrone 8" — and, again, "Madrosche does
    /// not have modes, just multiple chants". So every row below is his, not an
    /// inference. The note about gnize mode 7 carrying several chants is a fact
    /// about content rather than about this table: chants sharing a section and a
    /// mode are already distinguished by melody name in the identity index.
    /// </para>
    /// <para>
    /// Still not a closed set — he adds sections as he works through the treasury,
    /// so nothing may assume this list is complete.
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
        var eightOrdinalSections = new[]
        {
            "7a2c4b20-0000-4000-8000-000000000002", // Gnize
            "7a2c4b20-0000-4000-8000-000000000004", // Qonune yaunoye
            "7a2c4b20-0000-4000-8000-000000000005", // Tekso d-maurbe
            "7a2c4b20-0000-4000-8000-000000000006", // Mahebrone

            // Qole shahroye. ⚠️ The eight here is INFERRED, not stated: the owner
            // said only that "zodeq dnehwe, ... are qole shahroyo", and that
            // melody group demonstrably has at least a qadmoyo and a trayono
            // member — so the section has modes, and eight is the tradition's
            // ordinary count. It is not his word for it. Confirm and correct;
            // once section editing ships, that is a tick-box rather than a deploy.
            BethGazoSectionConfiguration.QoleShahroyeId,
        };

        foreach (var sectionId in eightOrdinalSections)
        {
            foreach (var modeId in eightOrdinals)
            {
                links.Add(new { SectionId = Guid.Parse(sectionId), ModeId = modeId });
            }
        }

        builder.HasData(links);
    }
}
