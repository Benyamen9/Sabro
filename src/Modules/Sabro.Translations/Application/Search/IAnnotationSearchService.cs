using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Translations.Application.Search;

public interface IAnnotationSearchService
{
    Task<Result<PagedResult<AnnotationSearchHitDto>>> SearchAsync(
        string? query,
        Guid? segmentId,
        Guid? sourceId,
        int? chapterNumber,
        int? verseNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
