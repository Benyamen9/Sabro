using Sabro.Shared.Results;

namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// Editing the modes of the Beth Gazo.
/// </summary>
/// <remarks>
/// <para>
/// The twin of <see cref="ISectionService"/>, and it exists for the same reason. The
/// modes were made a reference table rather than an enum precisely because the owner
/// said "some have more than eight, so make sure to have some margins" — and then
/// shipped read-only, so the one thing the table was built to allow was the one thing
/// nobody could do. Adding the ninth, <i>Mshaḥelfotho</i>, took a code change and a
/// deploy.
/// </para>
/// <para>
/// <b>Position is never set by hand</b>, for the same reason as the sections: it
/// carries a unique index, so an editor typing a slot already in use would only ever
/// be handed a constraint violation. New modes append; order changes by moving one
/// past its neighbour.
/// </para>
/// <para>
/// <b>Deleting is guarded harder than a section's.</b> A mode can be referenced from
/// two directions — by a chant, and by a section that admits it — and both have to be
/// clear before it can go.
/// </para>
/// </remarks>
public interface IModeService
{
    /// <summary>Creates a mode, appended after the last one.</summary>
    Task<Result<BethGazoModeDto>> CreateAsync(ModeRequest request, CancellationToken cancellationToken);

    /// <summary>Renames a mode. Its id never changes, so no chant's link is disturbed.</summary>
    Task<Result<BethGazoModeDto>> UpdateAsync(Guid id, ModeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a mode. Refused while any chant carries it, or any section admits it.
    /// </summary>
    Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Swaps a mode with its neighbour in the traditional order.</summary>
    Task<Error?> MoveAsync(Guid id, bool up, CancellationToken cancellationToken);
}
