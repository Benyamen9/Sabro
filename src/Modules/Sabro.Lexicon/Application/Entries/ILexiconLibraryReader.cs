namespace Sabro.Lexicon.Application.Entries;

/// <summary>
/// Read-only cross-module surface for the Meltho word library, consumed by the Play module.
/// Like <see cref="ILexiconPlayablePool"/> it does not re-check eligibility: a word that has
/// been served as a daily puzzle stays in the library even if later unpublished.
/// </summary>
public interface ILexiconLibraryReader
{
    /// <summary>
    /// Returns the list projection for the given entry ids (the word and its glosses).
    /// Order is unspecified and missing ids are simply absent — the caller (Play) keys the
    /// result by id and supplies the ordering. An empty input yields an empty list.
    /// </summary>
    Task<IReadOnlyList<LexiconLibraryListItem>> GetLibraryListAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the full detail projection (info fields + computed per-letter composition) for a
    /// single entry, or <c>null</c> if no entry with that id exists.
    /// </summary>
    Task<LexiconLibraryDetail?> GetLibraryDetailAsync(Guid id, CancellationToken cancellationToken);
}
