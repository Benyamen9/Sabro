using System.Globalization;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.Translations.Application.Search;

internal sealed class AnnotationSearchService : IAnnotationSearchService
{
    private readonly ISearchIndexQuery<AnnotationSearchDocument> searchIndex;
    private readonly ILogger<AnnotationSearchService> logger;

    public AnnotationSearchService(
        ISearchIndexQuery<AnnotationSearchDocument> searchIndex,
        ILogger<AnnotationSearchService> logger)
    {
        this.searchIndex = searchIndex;
        this.logger = logger;
    }

    public async Task<Result<PagedResult<AnnotationSearchHitDto>>> SearchAsync(
        string? query,
        Guid? segmentId,
        Guid? sourceId,
        int? chapterNumber,
        int? verseNumber,
        string? approvalStatus,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<AnnotationSearchHitDto>>.Failure(pageError);
        }

        var normalizedApprovalStatus = string.IsNullOrWhiteSpace(approvalStatus)
            ? null
            : approvalStatus.Trim().ToLowerInvariant();
        if (normalizedApprovalStatus is not null
            && normalizedApprovalStatus != "approved"
            && normalizedApprovalStatus != "rejected")
        {
            return Result<PagedResult<AnnotationSearchHitDto>>.Failure(
                Error.Validation("approvalStatus must be 'approved' or 'rejected'."));
        }

        var filters = BuildFilters(segmentId, sourceId, chapterNumber, verseNumber, normalizedApprovalStatus);
        var request = new SearchRequest(query, page, pageSize, filters);

        var response = await searchIndex.SearchAsync(request, cancellationToken);

        logger.LogInformation(
            "Annotation search executed. Query={Query} SegmentId={SegmentId} SourceId={SourceId} Chapter={Chapter} Verse={Verse} ApprovalStatus={ApprovalStatus} Page={Page} PageSize={PageSize} Total={Total}",
            query,
            segmentId,
            sourceId,
            chapterNumber,
            verseNumber,
            normalizedApprovalStatus,
            page,
            pageSize,
            response.Total);

        var hits = response.Items.Select(Map).ToArray();
        return Result<PagedResult<AnnotationSearchHitDto>>.Success(
            new PagedResult<AnnotationSearchHitDto>(hits, response.Total, response.Page, response.PageSize));
    }

    private static List<SearchFilter>? BuildFilters(Guid? segmentId, Guid? sourceId, int? chapterNumber, int? verseNumber, string? approvalStatus)
    {
        if (segmentId is null && sourceId is null && chapterNumber is null && verseNumber is null && approvalStatus is null)
        {
            return null;
        }

        var filters = new List<SearchFilter>(capacity: 5);
        if (segmentId is not null)
        {
            filters.Add(new SearchFilter("segmentId", segmentId.Value.ToString("D")));
        }

        if (sourceId is not null)
        {
            filters.Add(new SearchFilter("sourceId", sourceId.Value.ToString("D")));
        }

        if (chapterNumber is not null)
        {
            filters.Add(new SearchFilter("chapterNumber", chapterNumber.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (verseNumber is not null)
        {
            filters.Add(new SearchFilter("verseNumber", verseNumber.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (approvalStatus is not null)
        {
            filters.Add(new SearchFilter("approvalStatus", approvalStatus));
        }

        return filters;
    }

    private static AnnotationSearchHitDto Map(AnnotationSearchDocument doc) => new(
        Guid.Parse(doc.Id),
        Guid.Parse(doc.SegmentId),
        Guid.Parse(doc.SourceId),
        doc.ChapterNumber,
        doc.VerseNumber,
        doc.AnchorStart,
        doc.AnchorEnd,
        doc.Body,
        doc.Version,
        doc.ApprovalStatus);
}
