namespace Sabro.Lexicon.Application.Entries;

/// <summary>
/// Read-only cross-module surface over the Melthā-playable lexicon pool, consumed
/// by the Play module to select and render the daily puzzle. Kept narrow on purpose
/// — callers get the eligible-pool query and a by-id playable projection, not the
/// full entry CRUD surface.
/// </summary>
public interface ILexiconPlayablePool
{
    /// <summary>
    /// Returns the ids of entries currently eligible for Melthā: <c>Published</c>,
    /// <c>PlayableInMeltha</c>, and with a playable length within
    /// [<paramref name="minLength"/>, <paramref name="maxLength"/>] inclusive.
    /// Order is unspecified — the caller chooses among the returned candidates.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetEligibleEntryIdsAsync(int minLength, int maxLength, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the playable projection of a single entry by id, or <c>null</c> if
    /// no entry with that id exists. Deliberately does not re-check eligibility:
    /// once an entry has been served as a daily puzzle it must keep rendering even
    /// if it is later unpublished or unflagged. Selection (not rendering) is what
    /// the eligibility filter in <see cref="GetEligibleEntryIdsAsync"/> guards.
    /// </summary>
    Task<PlayableLexiconEntry?> GetPlayableEntryAsync(Guid id, CancellationToken cancellationToken);
}
