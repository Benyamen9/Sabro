using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Pagination;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Search;

namespace Sabro.UnitTests.Translations.Application.Search;

public class AnnotationSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithInvalidPage_ReturnsValidationErrorWithoutHittingIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<AnnotationSearchDocument>>();
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
        var index = Substitute.For<ISearchIndexQuery<AnnotationSearchDocument>>();
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

        await Search(service, query: "preposition", pageSize: 20);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r.Query == "preposition" && r.Page == 1 && r.PageSize == 20 && r.Filters == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithSegmentIdFilter_PassesGuidAsDashedString()
    {
        var segmentId = Guid.NewGuid();
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(service, segmentId: segmentId);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "segmentId"
                && r.Filters[0].Value == segmentId.ToString("D")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithSourceIdAndChapterFilters_PassesBoth()
    {
        var sourceId = Guid.NewGuid();
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(service, sourceId: sourceId, chapterNumber: 3);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 2
                && r.Filters[0].Field == "sourceId"
                && r.Filters[0].Value == sourceId.ToString("D")
                && r.Filters[1].Field == "chapterNumber"
                && r.Filters[1].Value == "3"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithVerseFilter_FormatsInvariantCulture()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(service, verseNumber: 16);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r =>
                r.Filters != null
                && r.Filters.Count == 1
                && r.Filters[0].Field == "verseNumber"
                && r.Filters[0].Value == "16"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WithAllFilters_PassesAllToIndex()
    {
        var index = StubEmptyIndex();
        var service = NewService(index);

        await Search(
            service,
            query: "logos",
            segmentId: Guid.NewGuid(),
            sourceId: Guid.NewGuid(),
            chapterNumber: 1,
            verseNumber: 1);

        await index.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r.Filters != null && r.Filters.Count == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_MapsDocumentsToHitDtos()
    {
        var annotationId = Guid.NewGuid();
        var segmentId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var doc = new AnnotationSearchDocument
        {
            Id = annotationId.ToString("D"),
            SegmentId = segmentId.ToString("D"),
            SourceId = sourceId.ToString("D"),
            ChapterNumber = 3,
            VerseNumber = 16,
            AnchorStart = 4,
            AnchorEnd = 12,
            Body = "Footnote on the term logos.",
            Version = 2,
            CreatedAtUnix = 1_700_000_000,
        };
        var index = Substitute.For<ISearchIndexQuery<AnnotationSearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AnnotationSearchDocument>(new[] { doc }, Total: 1, Page: 1, PageSize: 50));
        var service = NewService(index);

        var result = await Search(service, query: "logos");

        result.IsSuccess.Should().BeTrue();
        var hit = result.Value!.Items.Should().ContainSingle().Subject;
        hit.Id.Should().Be(annotationId);
        hit.SegmentId.Should().Be(segmentId);
        hit.SourceId.Should().Be(sourceId);
        hit.ChapterNumber.Should().Be(3);
        hit.VerseNumber.Should().Be(16);
        hit.AnchorStart.Should().Be(4);
        hit.AnchorEnd.Should().Be(12);
        hit.Body.Should().Be("Footnote on the term logos.");
        hit.Version.Should().Be(2);
    }

    private static AnnotationSearchService NewService(ISearchIndexQuery<AnnotationSearchDocument> index) =>
        new(index, NullLogger<AnnotationSearchService>.Instance);

    private static ISearchIndexQuery<AnnotationSearchDocument> StubEmptyIndex()
    {
        var index = Substitute.For<ISearchIndexQuery<AnnotationSearchDocument>>();
        index.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AnnotationSearchDocument>(
                Array.Empty<AnnotationSearchDocument>(),
                Total: 0,
                Page: 1,
                PageSize: 50));
        return index;
    }

    private static Task<Sabro.Shared.Results.Result<PagedResult<AnnotationSearchHitDto>>> Search(
        AnnotationSearchService service,
        string? query = null,
        Guid? segmentId = null,
        Guid? sourceId = null,
        int? chapterNumber = null,
        int? verseNumber = null,
        int page = 1,
        int pageSize = 50) =>
        service.SearchAsync(query, segmentId, sourceId, chapterNumber, verseNumber, page, pageSize, CancellationToken.None);
}
