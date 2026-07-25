using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Search;

/// <summary>
/// Admin-facing counterpart to <see cref="ILexiconSearchService"/>: searches the same
/// <c>lexicon</c> Meilisearch index (Draft and Published alike, since it is kept in sync
/// on every write) instead of the public service's hardcoded Published-only filter, and
/// maps to the full <see cref="LexiconEntryDto"/> the backoffice list needs rather than
/// the public <see cref="LexiconSearchHitDto"/>.
/// </summary>
public interface IAdminLexiconSearchService
{
    Task<Result<PagedResult<LexiconEntryDto>>> SearchAsync(
        string? query,
        LexiconEntryStatus? status,
        GrammaticalCategory? grammaticalCategory,
        bool? playableInMeltho,
        bool? hasPronunciationAudio,
        LexiconAdminSort sort,
        SortDirection? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
