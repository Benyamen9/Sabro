using System.Globalization;
using Microsoft.Extensions.Logging;
using Sabro.Biblical.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.Biblical.Application.Search;

internal sealed class BiblicalPassageSearchService : IBiblicalPassageSearchService
{
    private readonly ISearchIndexQuery<BiblicalPassageSearchDocument> searchIndex;
    private readonly ILogger<BiblicalPassageSearchService> logger;

    public BiblicalPassageSearchService(
        ISearchIndexQuery<BiblicalPassageSearchDocument> searchIndex,
        ILogger<BiblicalPassageSearchService> logger)
    {
        this.searchIndex = searchIndex;
        this.logger = logger;
    }

    public async Task<Result<PagedResult<BiblicalPassageSearchHitDto>>> SearchAsync(
        string? query,
        string? bookCode,
        Testament? testament,
        int? chapterNumber,
        int? verseNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<BiblicalPassageSearchHitDto>>.Failure(pageError);
        }

        var normalizedBookCode = string.IsNullOrWhiteSpace(bookCode) ? null : bookCode.Trim().ToUpperInvariant();
        var filters = BuildFilters(normalizedBookCode, testament, chapterNumber, verseNumber);
        var request = new SearchRequest(query, page, pageSize, filters);

        var response = await searchIndex.SearchAsync(request, cancellationToken);

        logger.LogInformation(
            "Biblical passage search executed. Query={Query} BookCode={BookCode} Testament={Testament} Chapter={Chapter} Verse={Verse} Page={Page} PageSize={PageSize} Total={Total}",
            query,
            normalizedBookCode,
            testament,
            chapterNumber,
            verseNumber,
            page,
            pageSize,
            response.Total);

        var hits = response.Items.Select(Map).ToArray();
        return Result<PagedResult<BiblicalPassageSearchHitDto>>.Success(
            new PagedResult<BiblicalPassageSearchHitDto>(hits, response.Total, response.Page, response.PageSize));
    }

    private static List<SearchFilter>? BuildFilters(string? bookCode, Testament? testament, int? chapterNumber, int? verseNumber)
    {
        if (bookCode is null && testament is null && chapterNumber is null && verseNumber is null)
        {
            return null;
        }

        var filters = new List<SearchFilter>(capacity: 4);
        if (bookCode is not null)
        {
            filters.Add(new SearchFilter("bookCode", bookCode));
        }

        if (testament is not null)
        {
            filters.Add(new SearchFilter("testament", testament.Value.ToString()));
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

    private static BiblicalPassageSearchHitDto Map(BiblicalPassageSearchDocument doc) => new(
        Guid.Parse(doc.Id),
        Guid.Parse(doc.BookId),
        doc.BookCode,
        doc.BookEnglishName,
        doc.BookSyriacName,
        Enum.Parse<Testament>(doc.Testament),
        doc.BookOrder,
        doc.ChapterNumber,
        doc.VerseNumber,
        doc.Reference);
}
