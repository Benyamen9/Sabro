using Sabro.Biblical.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Biblical.Application.Search;

public interface IBiblicalPassageSearchService
{
    Task<Result<PagedResult<BiblicalPassageSearchHitDto>>> SearchAsync(
        string? query,
        string? bookCode,
        Testament? testament,
        int? chapterNumber,
        int? verseNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
