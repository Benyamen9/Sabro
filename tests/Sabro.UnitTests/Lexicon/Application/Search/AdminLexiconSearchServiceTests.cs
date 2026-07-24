using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Search;

namespace Sabro.UnitTests.Lexicon.Application.Search;

public class AdminLexiconSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithInvalidPage_ReturnsValidationErrorWithoutHittingIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        var service = NewService(index);

        var result = await service.SearchAsync(
            query: null,
            status: null,
            grammaticalCategory: null,
            playableInMeltho: null,
            hasPronunciationAudio: null,
            sort: LexiconAdminSort.Recent,
            direction: null,
            page: 0,
            pageSize: 50,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().ContainKey("page");
        await index.DidNotReceive().SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithNoFilters_SendsNoStatusFilter()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(
            query: "ktb",
            status: null,
            grammaticalCategory: null,
            playableInMeltho: null,
            hasPronunciationAudio: null,
            sort: LexiconAdminSort.Recent,
            direction: null,
            page: 1,
            pageSize: 20,
            CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Query == "ktb"
                && r.Page == 1
                && r.PageSize == 20
                && r.Filters != null
                && r.Filters.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithStatusFilter_PassesStatusFilter()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(
            query: null,
            status: LexiconEntryStatus.Draft,
            grammaticalCategory: null,
            playableInMeltho: null,
            hasPronunciationAudio: null,
            sort: LexiconAdminSort.Recent,
            direction: null,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "status"
                && r.Filters[0].Value == "Draft"
                && !r.Filters[0].Raw),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithPlayableInMelthoFilter_PassesRawBooleanFilter()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(
            query: null,
            status: null,
            grammaticalCategory: null,
            playableInMeltho: true,
            hasPronunciationAudio: null,
            sort: LexiconAdminSort.Recent,
            direction: null,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "playableInMeltho"
                && r.Filters[0].Value == "true"
                && r.Filters[0].Raw),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithHasPronunciationAudioFalse_PassesRawBooleanFilter()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(
            query: null,
            status: null,
            grammaticalCategory: null,
            playableInMeltho: null,
            hasPronunciationAudio: false,
            sort: LexiconAdminSort.Recent,
            direction: null,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "hasPronunciationAudio"
                && r.Filters[0].Value == "false"
                && r.Filters[0].Raw),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithAllFilters_CombinesThemInOrder()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(
            query: null,
            status: LexiconEntryStatus.Published,
            grammaticalCategory: GrammaticalCategory.Verb,
            playableInMeltho: true,
            hasPronunciationAudio: true,
            sort: LexiconAdminSort.Recent,
            direction: null,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 4
                && r.Filters[0].Field == "status"
                && r.Filters[1].Field == "grammaticalCategory"
                && r.Filters[2].Field == "playableInMeltho"
                && r.Filters[3].Field == "hasPronunciationAudio"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(LexiconAdminSort.Recent, null, "createdAtUnix", true)]
    [InlineData(LexiconAdminSort.Syriac, null, "syriacUnvocalized", false)]
    [InlineData(LexiconAdminSort.Status, null, "status", false)]
    [InlineData(LexiconAdminSort.Length, null, "playableLength", false)]
    [InlineData(LexiconAdminSort.Recent, SortDirection.Ascending, "createdAtUnix", false)]
    [InlineData(LexiconAdminSort.Syriac, SortDirection.Descending, "syriacUnvocalized", true)]
    public async Task SearchAsync_MapsSortToExpectedFieldAndDirection(
        LexiconAdminSort sort,
        SortDirection? direction,
        string expectedField,
        bool expectedDescending)
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(
            query: null,
            status: null,
            grammaticalCategory: null,
            playableInMeltho: null,
            hasPronunciationAudio: null,
            sort: sort,
            direction: direction,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Sort != null
                && r.Sort.Count == 1
                && r.Sort[0].Field == expectedField
                && r.Sort[0].Descending == expectedDescending),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_MapsDocumentsToEntryDtosIncludingDraftStatus()
    {
        var id = Guid.NewGuid();
        var doc = new LexiconEntrySearchDocument
        {
            Id = id.ToString("D"),
            SyriacUnvocalized = "ܟܬܒ",
            SblTransliteration = "ktb",
            GrammaticalCategory = "Verb",
            Status = "Draft",
            MeaningTexts = new[] { "to write", "écrire" },
            MeaningLanguages = new[] { "en", "fr" },
            PlayableLength = 3,
            CreatedAtUnix = 1_700_000_000,
            UpdatedAtUnix = 1_700_000_100,
            HasPronunciationAudio = true,
            PronunciationAudioUrl = "/media/pronunciation/ktb.mp3",
        };
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LexiconEntrySearchDocument>(new[] { doc }, Total: 1, Page: 1, PageSize: 50));
        var service = NewService(index);

        var result = await service.SearchAsync(
            query: null,
            status: LexiconEntryStatus.Draft,
            grammaticalCategory: null,
            playableInMeltho: null,
            hasPronunciationAudio: null,
            sort: LexiconAdminSort.Recent,
            direction: null,
            page: 1,
            pageSize: 50,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value!.Items.Should().ContainSingle().Subject;
        entry.Id.Should().Be(id);
        entry.Status.Should().Be(LexiconEntryStatus.Draft);
        entry.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
        entry.Meanings.Should().HaveCount(2);
        entry.Meanings[0].Language.Should().Be("en");
        entry.Meanings[0].Text.Should().Be("to write");
        entry.PronunciationAudioUrl.Should().Be("/media/pronunciation/ktb.mp3");
        entry.CreatedAt.ToUnixTimeSeconds().Should().Be(1_700_000_000);
        entry.UpdatedAt.ToUnixTimeSeconds().Should().Be(1_700_000_100);
    }

    private static AdminLexiconSearchService NewService(ISearchIndexQuery<LexiconEntrySearchDocument> index) =>
        new(index, NullLogger<AdminLexiconSearchService>.Instance);

    private static ISearchIndexQuery<LexiconEntrySearchDocument> StubEmptyIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LexiconEntrySearchDocument>(Array.Empty<LexiconEntrySearchDocument>(), Total: 0, Page: 1, PageSize: 50));
        return index;
    }
}
