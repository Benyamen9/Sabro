using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Dictionary;

/// <summary>
/// The public dictionary read surface: every <em>published</em> entry, browsable
/// alphabetically — the hub's general library, next to the Meltho library that
/// only shows served words. Search goes through <c>ILexiconSearchService</c>
/// (Meilisearch, already published-only); this service covers browse + detail
/// from the relational source of truth.
/// </summary>
public interface IDictionaryService
{
    /// <summary>
    /// Lists published entries alphabetically by their unvocalized Syriac form
    /// (ICU collation), paged, optionally filtered to one grammatical category.
    /// </summary>
    Task<Result<PagedResult<DictionaryEntryListItem>>> ListAsync(
        int page,
        int pageSize,
        GrammaticalCategory? category,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the full detail projection (info fields + per-letter composition,
    /// same shape the Meltho library uses) for one published entry. Not-found for
    /// drafts and unknown ids alike — a draft's existence is editorial state.
    /// </summary>
    Task<Result<LexiconLibraryDetail>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
