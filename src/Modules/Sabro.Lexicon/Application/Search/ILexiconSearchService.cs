using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Search;

public interface ILexiconSearchService
{
    Task<Result<PagedResult<LexiconSearchHitDto>>> SearchAsync(
        string? query,
        GrammaticalCategory? grammaticalCategory,
        Guid? rootId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
