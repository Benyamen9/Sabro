using Microsoft.Extensions.Logging;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.Lexicon.Application.Search;

internal sealed class LexiconSearchService : ILexiconSearchService
{
    private readonly ISearchIndexQuery<LexiconEntrySearchDocument> searchIndex;
    private readonly ILogger<LexiconSearchService> logger;

    public LexiconSearchService(
        ISearchIndexQuery<LexiconEntrySearchDocument> searchIndex,
        ILogger<LexiconSearchService> logger)
    {
        this.searchIndex = searchIndex;
        this.logger = logger;
    }

    public async Task<Result<PagedResult<LexiconSearchHitDto>>> SearchAsync(
        string? query,
        GrammaticalCategory? grammaticalCategory,
        Guid? rootId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<LexiconSearchHitDto>>.Failure(pageError);
        }

        var filters = BuildFilters(grammaticalCategory, rootId);
        var request = new SearchRequest(query, page, pageSize, filters);

        var response = await searchIndex.SearchAsync(request, cancellationToken);

        logger.LogInformation(
            "Lexicon search executed. Query={Query} Category={Category} RootId={RootId} Page={Page} PageSize={PageSize} Total={Total}",
            query,
            grammaticalCategory,
            rootId,
            page,
            pageSize,
            response.Total);

        var hits = response.Items.Select(Map).ToArray();
        return Result<PagedResult<LexiconSearchHitDto>>.Success(
            new PagedResult<LexiconSearchHitDto>(hits, response.Total, response.Page, response.PageSize));
    }

    private static List<SearchFilter> BuildFilters(GrammaticalCategory? grammaticalCategory, Guid? rootId)
    {
        // Public search only ever exposes published entries — drafts are editorial state.
        var filters = new List<SearchFilter>(capacity: 3)
        {
            new SearchFilter("status", nameof(LexiconEntryStatus.Published)),
        };

        if (grammaticalCategory is not null)
        {
            filters.Add(new SearchFilter("grammaticalCategory", grammaticalCategory.Value.ToString()));
        }

        if (rootId is not null)
        {
            filters.Add(new SearchFilter("rootId", rootId.Value.ToString("D")));
        }

        return filters;
    }

    private static LexiconSearchHitDto Map(LexiconEntrySearchDocument doc) => new(
        Guid.Parse(doc.Id),
        doc.SyriacUnvocalized,
        doc.SyriacVocalized,
        doc.SblTransliteration,
        doc.TransliterationVariants,
        string.IsNullOrEmpty(doc.RootId) ? null : Guid.Parse(doc.RootId),
        doc.RootForm,
        doc.GrammaticalCategory,
        doc.Morphology,
        doc.MeaningTexts,
        doc.MeaningLanguages);
}
