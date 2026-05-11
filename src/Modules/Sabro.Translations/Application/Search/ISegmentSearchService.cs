using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Search;

public interface ISegmentSearchService
{
    Task<Result<PagedResult<SegmentSearchHitDto>>> SearchAsync(
        string? query,
        Guid? sourceId,
        int? chapterNumber,
        int? verseNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
