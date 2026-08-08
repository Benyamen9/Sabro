using Sabro.Shared.Results;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// Editing the sections of the treasury, and which modes each admits.
/// </summary>
/// <remarks>
/// <para>
/// This exists because the sections were a reference table with no door to it. They
/// were deliberately modelled as data rather than as an enum — "a row an editor adds,
/// not a deploy" — and then shipped read-only, so every correction was a migration.
/// On 2026-08-08 alone that cost four deploys: <i>Qole shahroye</i>, the four sections
/// completing the book's ten, and two schema changes on top.
/// </para>
/// <para>
/// <b>Position is never set by hand.</b> A new section is appended, and reordering is
/// a swap of two neighbours. The column carries a unique index — two sections sharing
/// a slot would make the order ambiguous — and letting an editor type a number would
/// mean handing them a constraint violation whenever they picked one already in use.
/// </para>
/// </remarks>
public interface ISectionService
{
    /// <summary>Creates a section, appended after the last one.</summary>
    Task<Result<BethGazoSectionDto>> CreateAsync(SectionRequest request, CancellationToken cancellationToken);

    /// <summary>Renames a section and replaces the set of modes it admits.</summary>
    Task<Result<BethGazoSectionDto>> UpdateAsync(Guid id, SectionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a section. Refused while any chant belongs to it — the foreign key is
    /// Restrict, and a raw violation would tell the editor nothing about why.
    /// </summary>
    Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Swaps a section with its neighbour in the treasury's order. <paramref name="up"/>
    /// moves it earlier.
    /// </summary>
    Task<Error?> MoveAsync(Guid id, bool up, CancellationToken cancellationToken);
}
