using Microsoft.Extensions.Logging;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.Lexicon.Application.Search;

internal sealed class AdminLexiconSearchService : IAdminLexiconSearchService
{
    private readonly ISearchIndexQuery<LexiconEntrySearchDocument> searchIndex;
    private readonly ILogger<AdminLexiconSearchService> logger;

    public AdminLexiconSearchService(
        ISearchIndexQuery<LexiconEntrySearchDocument> searchIndex,
        ILogger<AdminLexiconSearchService> logger)
    {
        this.searchIndex = searchIndex;
        this.logger = logger;
    }

    public async Task<Result<PagedResult<LexiconEntryDto>>> SearchAsync(
        string? query,
        LexiconEntryStatus? status,
        GrammaticalCategory? grammaticalCategory,
        bool? playableInMeltho,
        bool? hasPronunciationAudio,
        LexiconAdminSort sort,
        SortDirection? direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<LexiconEntryDto>>.Failure(pageError);
        }

        var filters = BuildFilters(status, grammaticalCategory, playableInMeltho, hasPronunciationAudio);
        var sortSpec = BuildSort(sort, direction);
        var request = new SearchRequest(query, page, pageSize, filters, sortSpec);

        var response = await searchIndex.SearchAsync(request, cancellationToken);

        logger.LogInformation(
            "Admin lexicon search executed. Query={Query} Status={Status} Category={Category} Playable={Playable} " +
            "HasPronunciation={HasPronunciation} Sort={Sort} Direction={Direction} Page={Page} PageSize={PageSize} Total={Total}",
            query,
            status,
            grammaticalCategory,
            playableInMeltho,
            hasPronunciationAudio,
            sort,
            direction,
            page,
            pageSize,
            response.Total);

        var items = response.Items.Select(Map).ToArray();
        return Result<PagedResult<LexiconEntryDto>>.Success(
            new PagedResult<LexiconEntryDto>(items, response.Total, response.Page, response.PageSize));
    }

    // Unlike the public LexiconSearchService, there is no hardcoded status filter here —
    // the backoffice must see Draft rows (that is the whole point of the SEDRA-triage use case).
    private static List<SearchFilter> BuildFilters(
        LexiconEntryStatus? status,
        GrammaticalCategory? grammaticalCategory,
        bool? playableInMeltho,
        bool? hasPronunciationAudio)
    {
        var filters = new List<SearchFilter>(capacity: 4);

        if (status is not null)
        {
            filters.Add(new SearchFilter("status", status.Value.ToString()));
        }

        if (grammaticalCategory is not null)
        {
            filters.Add(new SearchFilter("grammaticalCategory", grammaticalCategory.Value.ToString()));
        }

        if (playableInMeltho is not null)
        {
            filters.Add(new SearchFilter("playableInMeltho", playableInMeltho.Value ? "true" : "false", Raw: true));
        }

        if (hasPronunciationAudio is not null)
        {
            filters.Add(new SearchFilter("hasPronunciationAudio", hasPronunciationAudio.Value ? "true" : "false", Raw: true));
        }

        return filters;
    }

    private static SearchSort[] BuildSort(LexiconAdminSort sort, SortDirection? direction)
    {
        var descending = (direction ?? NaturalDirection(sort)) == SortDirection.Descending;
        var field = sort switch
        {
            LexiconAdminSort.Syriac => "syriacUnvocalized",
            LexiconAdminSort.Status => "status",
            LexiconAdminSort.Length => "playableLength",
            _ => "createdAtUnix",
        };

        return new[] { new SearchSort(field, descending) };
    }

    // Recent reads newest-first; the textual/status/length sorts read smallest/alphabetical-first.
    private static SortDirection NaturalDirection(LexiconAdminSort sort) =>
        sort == LexiconAdminSort.Recent ? SortDirection.Descending : SortDirection.Ascending;

    private static LexiconEntryDto Map(LexiconEntrySearchDocument doc) => new(
        Guid.Parse(doc.Id),
        doc.SyriacUnvocalized,
        doc.SyriacVocalized,
        string.IsNullOrEmpty(doc.RootId) ? null : Guid.Parse(doc.RootId),
        doc.SblTransliteration,
        doc.TransliterationVariants,
        Enum.Parse<GrammaticalCategory>(doc.GrammaticalCategory),
        doc.Morphology,
        MapMeanings(doc),
        Enum.Parse<LexiconEntryStatus>(doc.Status),
        doc.PlayableInMeltho,
        doc.PronunciationAudioUrl,
        doc.PlayableLength,
        DateTimeOffset.FromUnixTimeSeconds(doc.CreatedAtUnix),
        DateTimeOffset.FromUnixTimeSeconds(doc.UpdatedAtUnix));

    private static LexiconMeaningDto[] MapMeanings(LexiconEntrySearchDocument doc) =>
        doc.MeaningLanguages
            .Zip(doc.MeaningTexts, (language, text) => new LexiconMeaningDto(language, text))
            .ToArray();
}
