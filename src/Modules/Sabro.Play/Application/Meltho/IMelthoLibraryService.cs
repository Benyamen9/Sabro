using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltho;

/// <summary>
/// Reads the public Meltho word library — the words served on past days, joined to their
/// lexicon detail. Today's word is never exposed (it would spoil the live puzzle). Words are
/// deduplicated across the days they appeared: the list is per word, the detail lists every
/// past date.
/// </summary>
public interface IMelthoLibraryService
{
    /// <summary>
    /// Lists past words, paged, in the requested <paramref name="sort"/> order and
    /// <paramref name="direction"/> (most recently served first by default; a null direction
    /// applies the field's natural default). When <paramref name="search"/> is non-empty, only
    /// words whose Syriac form, transliteration, or any gloss contains it (case- and
    /// diacritic-insensitive) are returned, and the total reflects the filtered set. Validates
    /// pagination and returns a per-word page (each word once, with the date it was last served
    /// and how many past days it appeared on).
    /// </summary>
    Task<Result<PagedResult<MelthoLibraryEntryDto>>> ListAsync(int page, int pageSize, LibrarySort sort, SortDirection? direction, string? search, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the detail for one past word (info + composition + all past dates). Fails with
    /// not-found if the word has never been served on a past day, or no longer resolves.
    /// </summary>
    Task<Result<MelthoLibraryDetailDto>> GetDetailAsync(Guid lexiconEntryId, CancellationToken cancellationToken);
}
