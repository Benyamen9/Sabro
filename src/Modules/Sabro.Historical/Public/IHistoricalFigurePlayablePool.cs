namespace Sabro.Historical.Public;

/// <summary>
/// Read-only cross-module surface over the Shmo-playable figure pool, consumed by
/// the Play module to select and render the daily puzzle. Kept narrow on purpose
/// — callers get the eligible-pool query and a by-id playable projection, not the
/// full figure CRUD surface.
/// </summary>
public interface IHistoricalFigurePlayablePool
{
    /// <summary>
    /// Returns the ids of figures currently eligible for Shmo: <c>Published</c> and
    /// <c>PlayableInShmo</c>. Unlike the Lexicon pool there is no length window —
    /// a name's length is not a constraint on the game. Order is unspecified — the
    /// caller chooses among the returned candidates.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetEligibleFigureIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the playable projection of a single figure by id, or <c>null</c> if no
    /// figure with that id exists. Deliberately does not re-check eligibility: once a
    /// figure has been served as a daily puzzle it must keep rendering even if it is
    /// later unpublished or unflagged. Selection (not rendering) is what the
    /// eligibility filter in <see cref="GetEligibleFigureIdsAsync"/> guards.
    /// </summary>
    Task<PlayableHistoricalFigure?> GetPlayableFigureAsync(Guid id, CancellationToken cancellationToken);
}
