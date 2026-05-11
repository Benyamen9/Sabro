using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Search;

namespace Sabro.UnitTests.Lexicon.Application.Search;

public class LexiconSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithInvalidPage_ReturnsValidationErrorWithoutHittingIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        var service = NewService(index);

        var result = await service.SearchAsync(query: null, grammaticalCategory: null, rootId: null, page: 0, pageSize: 50, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().ContainKey("page");
        await index.DidNotReceive().SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithExcessivePageSize_ReturnsValidationError()
    {
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        var service = NewService(index);

        var result = await service.SearchAsync(query: null, grammaticalCategory: null, rootId: null, page: 1, pageSize: PageRequest.MaxPageSize + 1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Fields.Should().ContainKey("pageSize");
    }

    [Fact]
    public async Task SearchAsync_WithNoFilters_PassesNullFiltersToIndex()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(query: "ktb", grammaticalCategory: null, rootId: null, page: 1, pageSize: 20, CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r.Query == "ktb" && r.Page == 1 && r.PageSize == 20 && r.Filters == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithCategoryFilter_PassesEqualsFilterToIndex()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(query: null, grammaticalCategory: GrammaticalCategory.Verb, rootId: null, page: 1, pageSize: 50, CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "grammaticalCategory"
                && r.Filters[0].Value == "Verb"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithRootIdFilter_PassesGuidAsDashedString()
    {
        var rootId = Guid.NewGuid();
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(query: null, grammaticalCategory: null, rootId: rootId, page: 1, pageSize: 50, CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "rootId"
                && r.Filters[0].Value == rootId.ToString("D")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithBothFilters_PassesBothToIndex()
    {
        var rootId = Guid.NewGuid();
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(query: null, grammaticalCategory: GrammaticalCategory.Noun, rootId: rootId, page: 1, pageSize: 50, CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r.Filters != null && r.Filters.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_MapsDocumentsToHitDtos()
    {
        var id = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var doc = new LexiconEntrySearchDocument
        {
            Id = id.ToString("D"),
            SyriacUnvocalized = "ܟܬܒ",
            SyriacVocalized = "ܟܳܬܶܒ",
            SblTransliteration = "ktb",
            TransliterationVariants = new[] { "kthab" },
            RootId = rootId.ToString("D"),
            RootForm = "ܟܬܒ",
            GrammaticalCategory = "Verb",
            Morphology = "Pe'al",
            MeaningTexts = new[] { "to write" },
            MeaningLanguages = new[] { "en" },
        };
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LexiconEntrySearchDocument>(new[] { doc }, Total: 1, Page: 1, PageSize: 50));
        var service = NewService(index);

        var result = await service.SearchAsync(query: "ktb", grammaticalCategory: null, rootId: null, page: 1, pageSize: 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value!.Items.Should().ContainSingle().Subject;
        hit.Id.Should().Be(id);
        hit.RootId.Should().Be(rootId);
        hit.RootForm.Should().Be("ܟܬܒ");
        hit.GrammaticalCategory.Should().Be("Verb");
        hit.MeaningTexts.Should().ContainSingle().Which.Should().Be("to write");
        result.Value.Total.Should().Be(1);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyRootIdInDocument_LeavesRootIdNull()
    {
        var id = Guid.NewGuid();
        var doc = new LexiconEntrySearchDocument
        {
            Id = id.ToString("D"),
            SyriacUnvocalized = "ܫܠܡܐ",
            SblTransliteration = "šlāmā",
            GrammaticalCategory = "Noun",
            RootId = null,
        };
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LexiconEntrySearchDocument>(new[] { doc }, Total: 1, Page: 1, PageSize: 50));
        var service = NewService(index);

        var result = await service.SearchAsync(query: null, grammaticalCategory: null, rootId: null, page: 1, pageSize: 50, CancellationToken.None);

        result.Value!.Items.Should().ContainSingle().Which.RootId.Should().BeNull();
    }

    private static LexiconSearchService NewService(ISearchIndexQuery<LexiconEntrySearchDocument> index) =>
        new(index, NullLogger<LexiconSearchService>.Instance);

    private static ISearchIndexQuery<LexiconEntrySearchDocument> StubEmptyIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<LexiconEntrySearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<LexiconEntrySearchDocument>(Array.Empty<LexiconEntrySearchDocument>(), Total: 0, Page: 1, PageSize: 50));
        return index;
    }
}
