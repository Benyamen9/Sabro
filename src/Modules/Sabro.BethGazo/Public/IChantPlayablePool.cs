namespace Sabro.BethGazo.Public;

/// <summary>
/// Read-only cross-module surface over the Nahlo-playable chant pool, consumed by
/// the Play module to select and render the daily puzzle. Kept narrow on purpose
/// — callers get the eligible-pool query and a by-id playable projection, not the
/// full chant CRUD surface.
/// </summary>
public interface IChantPlayablePool
{
    /// <summary>
    /// Returns the ids of chants currently eligible for Nahlo: <c>Published</c> and
    /// <c>PlayableInNahlo</c>. As with the Shmo roster there is no length window —
    /// a melody's length is not a constraint on the game. Order is unspecified —
    /// the caller chooses among the returned candidates.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetEligibleChantIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the playable projection of a single chant by id, or <c>null</c> if no
    /// chant with that id exists. Deliberately does not re-check eligibility: once a
    /// chant has been served as a daily puzzle it must keep rendering even if it is
    /// later unpublished or unflagged. Selection (not rendering) is what the
    /// eligibility filter in <see cref="GetEligibleChantIdsAsync"/> guards.
    /// </summary>
    Task<PlayableChant?> GetPlayableChantAsync(Guid id, CancellationToken cancellationToken);
}
