using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Pagination;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Search;

namespace Sabro.UnitTests.Translations.Application.Search;

public class SegmentSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithInvalidPage_ReturnsValidationErrorWithoutHittingIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<SegmentSearchDocument>>();
        var service = NewService(index);

        var result = await service.SearchAsync(query: null, sourceId: null, chapterNumber: null, verseNumber: null, page: 0, pageSize: 50, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Fields.Should().ContainKey("page");
        await index.DidNotReceive().SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithNoFilters_PassesNullFiltersToIndex()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(query: "principio", sourceId: null, chapterNumber: null, verseNumber: null, page: 1, pageSize: 20, CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r.Query == "principio" && r.Filters == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithSourceIdFilter_PassesGuidAsDashedString()
    {
        var sourceId = Guid.NewGuid();
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(query: null, sourceId: sourceId, chapterNumber: null, verseNumber: null, page: 1, pageSize: 50, CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "sourceId"
                && r.Filters[0].Value == sourceId.ToString("D")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithChapterAndVerseFilters_PassesInvariantIntegers()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await service.SearchAsync(query: null, sourceId: null, chapterNumber: 3, verseNumber: 16, page: 1, pageSize: 50, CancellationToken.None);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 2
                && r.Filters[0].Field == "chapterNumber" && r.Filters[0].Value == "3"
                && r.Filters[1].Field == "verseNumber" && r.Filters[1].Value == "16"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_MapsDocumentsToHitDtos()
    {
        var id = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var textVersionId = Guid.NewGuid();
        var doc = new SegmentSearchDocument
        {
            Id = id.ToString("D"),
            SourceId = sourceId.ToString("D"),
            ChapterNumber = 1,
            VerseNumber = 1,
            TextVersionId = textVersionId.ToString("D"),
            Content = "In the beginning",
            Version = 2,
        };
        var index = Substitute.For<ISearchIndexQuery<SegmentSearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SegmentSearchDocument>(new[] { doc }, Total: 1, Page: 1, PageSize: 50));
        var service = NewService(index);

        var result = await service.SearchAsync(query: "beginning", sourceId: null, chapterNumber: null, verseNumber: null, page: 1, pageSize: 50, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value!.Items.Should().ContainSingle().Subject;
        hit.Id.Should().Be(id);
        hit.SourceId.Should().Be(sourceId);
        hit.TextVersionId.Should().Be(textVersionId);
        hit.ChapterNumber.Should().Be(1);
        hit.VerseNumber.Should().Be(1);
        hit.Content.Should().Be("In the beginning");
        hit.Version.Should().Be(2);
    }

    private static SegmentSearchService NewService(ISearchIndexQuery<SegmentSearchDocument> index) =>
        new(index, NullLogger<SegmentSearchService>.Instance);

    private static ISearchIndexQuery<SegmentSearchDocument> StubEmptyIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<SegmentSearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SegmentSearchDocument>(Array.Empty<SegmentSearchDocument>(), Total: 0, Page: 1, PageSize: 50));
        return index;
    }
}
