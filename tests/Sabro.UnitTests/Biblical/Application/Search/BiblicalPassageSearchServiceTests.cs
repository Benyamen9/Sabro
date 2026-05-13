using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Search;

namespace Sabro.UnitTests.Biblical.Application.Search;

public class BiblicalPassageSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithInvalidPage_ReturnsValidationErrorWithoutHittingIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<BiblicalPassageSearchDocument>>();
        var service = NewService(index);

        var result = await Search(service, page: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
        result.Error.Fields.Should().ContainKey("page");
        await index.DidNotReceive().SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithExcessivePageSize_ReturnsValidationError()
    {
        var index = Substitute.For<ISearchIndexQuery<BiblicalPassageSearchDocument>>();
        var service = NewService(index);

        var result = await Search(service, pageSize: PageRequest.MaxPageSize + 1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Fields.Should().ContainKey("pageSize");
    }

    [Fact]
    public async Task SearchAsync_WithNoFilters_PassesNullFiltersToIndex()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(service, query: "matthew", pageSize: 20);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r.Query == "matthew" && r.Page == 1 && r.PageSize == 20 && r.Filters == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithBookCodeFilter_NormalizesToUpperInvariant()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(service, bookCode: " mat ");

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "bookCode"
                && r.Filters[0].Value == "MAT"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithTestamentFilter_PassesEnumNameToIndex()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(service, testament: Testament.Old);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "testament"
                && r.Filters[0].Value == "Old"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithChapterAndVerseFilters_PassesBothInInvariantCulture()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(service, chapterNumber: 3, verseNumber: 16);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 2
                && r.Filters[0].Field == "chapterNumber"
                && r.Filters[0].Value == "3"
                && r.Filters[1].Field == "verseNumber"
                && r.Filters[1].Value == "16"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithAllFilters_PassesAllToIndex()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(
            service,
            query: "john",
            bookCode: "JHN",
            testament: Testament.New,
            chapterNumber: 3,
            verseNumber: 16);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r.Filters != null && r.Filters.Count == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_MapsDocumentsToHitDtos()
    {
        var passageId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var doc = new BiblicalPassageSearchDocument
        {
            Id = passageId.ToString("D"),
            BookId = bookId.ToString("D"),
            BookCode = "MAT",
            BookEnglishName = "Matthew",
            BookSyriacName = "ܡܬܝ",
            Testament = "New",
            BookOrder = 40,
            ChapterNumber = 3,
            VerseNumber = 7,
            Reference = "Matthew 3:7",
        };
        var index = Substitute.For<ISearchIndexQuery<BiblicalPassageSearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BiblicalPassageSearchDocument>(new[] { doc }, Total: 1, Page: 1, PageSize: 50));
        var service = NewService(index);

        var result = await Search(service, query: "matthew");

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value!.Items.Should().ContainSingle().Subject;
        hit.Id.Should().Be(passageId);
        hit.BookId.Should().Be(bookId);
        hit.BookCode.Should().Be("MAT");
        hit.BookEnglishName.Should().Be("Matthew");
        hit.BookSyriacName.Should().Be("ܡܬܝ");
        hit.Testament.Should().Be(Testament.New);
        hit.BookOrder.Should().Be(40);
        hit.ChapterNumber.Should().Be(3);
        hit.VerseNumber.Should().Be(7);
        hit.Reference.Should().Be("Matthew 3:7");
    }

    [Fact]
    public async Task SearchAsync_WithNullSyriacName_LeavesNullOnHit()
    {
        var doc = new BiblicalPassageSearchDocument
        {
            Id = Guid.NewGuid().ToString("D"),
            BookId = Guid.NewGuid().ToString("D"),
            BookCode = "GEN",
            BookEnglishName = "Genesis",
            BookSyriacName = null,
            Testament = "Old",
            BookOrder = 1,
            ChapterNumber = 1,
            VerseNumber = 1,
            Reference = "Genesis 1:1",
        };
        var index = Substitute.For<ISearchIndexQuery<BiblicalPassageSearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BiblicalPassageSearchDocument>(new[] { doc }, Total: 1, Page: 1, PageSize: 50));
        var service = NewService(index);

        var result = await Search(service);

        result.Value!.Items.Should().ContainSingle().Which.BookSyriacName.Should().BeNull();
    }

    private static BiblicalPassageSearchService NewService(ISearchIndexQuery<BiblicalPassageSearchDocument> index) =>
        new(index, NullLogger<BiblicalPassageSearchService>.Instance);

    private static ISearchIndexQuery<BiblicalPassageSearchDocument> StubEmptyIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<BiblicalPassageSearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BiblicalPassageSearchDocument>(
                Array.Empty<BiblicalPassageSearchDocument>(),
                Total: 0,
                Page: 1,
                PageSize: 50));
        return index;
    }

    private static Task<Sabro.Shared.Results.Result<PagedResult<BiblicalPassageSearchHitDto>>> Search(
        BiblicalPassageSearchService service,
        string? query = null,
        string? bookCode = null,
        Testament? testament = null,
        int? chapterNumber = null,
        int? verseNumber = null,
        int page = 1,
        int pageSize = 50) =>
        service.SearchAsync(query, bookCode, testament, chapterNumber, verseNumber, page, pageSize, CancellationToken.None);
}
