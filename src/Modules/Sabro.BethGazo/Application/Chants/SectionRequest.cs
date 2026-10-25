namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A section as the backoffice submits it.
/// </summary>
/// <remarks>
/// No position: a new section is appended and reordering is a separate move, so an
/// editor never types a number into a uniquely-indexed column. See
/// <see cref="ISectionService"/>.
/// <para>
/// A full replacement: omitting the Syriac name or the descriptions clears them, so
/// the backoffice sends back what it was given.
/// </para>
/// </remarks>
/// <param name="AllowedModeIds">
/// The modes this section admits. <b>An empty list is meaningful</b> — it declares a
/// section with no modes, which is how the madroshe are expressed, and not a field
/// left blank.
/// </param>
public sealed record SectionRequest(
    string Name,
    IReadOnlyList<Guid> AllowedModeIds,
    string? SyriacName = null,
    IReadOnlyList<BethGazoDescriptionRequest>? Descriptions = null);
