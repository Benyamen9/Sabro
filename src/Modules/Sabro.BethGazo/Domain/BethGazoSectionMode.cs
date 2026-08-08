namespace Sabro.BethGazo.Domain;

/// <summary>
/// One mode admitted by one section — the link that carries both of the owner's
/// rules about which modes a chant may have.
/// </summary>
/// <remarks>
/// A plain link rather than a flag on either side, because the rules are about the
/// <i>pair</i>: the mshaḥelfotho is a perfectly real mode that only the farde use,
/// and the madroshe are a perfectly real section that use no mode. Neither fact
/// belongs to the mode alone or the section alone.
/// </remarks>
public sealed class BethGazoSectionMode
{
    internal BethGazoSectionMode(Guid sectionId, Guid modeId)
    {
        SectionId = sectionId;
        ModeId = modeId;
    }

    private BethGazoSectionMode()
    {
    }

    public Guid SectionId { get; private set; }

    public Guid ModeId { get; private set; }
}
