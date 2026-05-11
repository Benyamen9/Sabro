using System.Globalization;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.Translations.Application.Search;

internal sealed class SegmentSearchService : ISegmentSearchService
{
    private readonly ISearchIndexQuery<SegmentSearchDocument> searchIndex;
    private readonly ILogger<SegmentSearchService> logger;

    public SegmentSearchService(
        ISearchIndexQuery<SegmentSearchDocument> searchIndex,
        ILogger<SegmentSearchService> logger)
    {
        this.searchIndex = searchIndex;
        this.logger = logger;
    }

    public async Task<Result<PagedResult<SegmentSearchHitDto>>> SearchAsync(
        string? query,
        Guid? sourceId,
        int? chapterNumber,
        int? verseNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<SegmentSearchHitDto>>.Failure(pageError);
        }

        var filters = BuildFilters(sourceId, chapterNumber, verseNumber);
        var request = new SearchRequest(query, page, pageSize, filters);

        var response = await searchIndex.SearchAsync(request, cancellationToken);

        logger.LogInformation(
            "Segment search executed. Query={Query} SourceId={SourceId} Chapter={Chapter} Verse={Verse} Page={Page} PageSize={PageSize} Total={Total}",
            query,
            sourceId,
            chapterNumber,
            verseNumber,
            page,
            pageSize,
            response.Total);

        var hits = response.Items.Select(Map).ToArray();
        return Result<PagedResult<SegmentSearchHitDto>>.Success(
            new PagedResult<SegmentSearchHitDto>(hits, response.Total, response.Page, response.PageSize));
    }

    private static List<SearchFilter>? BuildFilters(Guid? sourceId, int? chapterNumber, int? verseNumber)
    {
        if (sourceId is null && chapterNumber is null && verseNumber is null)
        {
            return null;
        }

        var filters = new List<SearchFilter>(capacity: 3);
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

        return filters;
    }

    private static SegmentSearchHitDto Map(SegmentSearchDocument doc) => new(
        Guid.Parse(doc.Id),
        Guid.Parse(doc.SourceId),
        doc.ChapterNumber,
        doc.VerseNumber,
        Guid.Parse(doc.TextVersionId),
        doc.Content,
        doc.Version);
}
