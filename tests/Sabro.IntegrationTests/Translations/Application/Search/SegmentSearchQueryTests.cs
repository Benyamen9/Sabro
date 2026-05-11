using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Search;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Translations.Application.Search;

[Collection(TranslationsCollection.Name)]
public class SegmentSearchQueryTests
{
    private readonly MeilisearchFixture meili;

    public SegmentSearchQueryTests(MeilisearchFixture meili)
    {
        this.meili = meili;
    }

    [Fact]
    public async Task SearchAsync_OverFreeTextQuery_ReturnsIndexedSegment()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var segment = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "In principio erat verbum").Value!;
        var doc = SegmentDocumentMapper.Map(segment);
        await client.Index(indexName).AddDocumentsAsync(new[] { doc }, descriptor.PrimaryKey, ct);

        var service = NewService(client, descriptor);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync("verbum", sourceId: null, chapterNumber: null, verseNumber: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.Content.Should().Be("In principio erat verbum");
    }

    [Fact]
    public async Task SearchAsync_WithSourceIdFilter_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var sourceA = Guid.NewGuid();
        var sourceB = Guid.NewGuid();
        var textVersionId = Guid.NewGuid();
        var a = Segment.Create(sourceA, 1, 1, textVersionId, "In the beginning").Value!;
        var b = Segment.Create(sourceB, 1, 1, textVersionId, "In the beginning").Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[] { SegmentDocumentMapper.Map(a), SegmentDocumentMapper.Map(b) },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync("beginning", sourceId: sourceA, chapterNumber: null, verseNumber: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.SourceId.Should().Be(sourceA);
    }

    [Fact]
    public async Task SearchAsync_WithChapterAndVerseFilters_RestrictsToExactPosition()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var sourceId = Guid.NewGuid();
        var textVersionId = Guid.NewGuid();
        var s1 = Segment.Create(sourceId, 1, 1, textVersionId, "First verse").Value!;
        var s2 = Segment.Create(sourceId, 2, 3, textVersionId, "Target verse").Value!;
        var s3 = Segment.Create(sourceId, 2, 4, textVersionId, "Next verse").Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[] { SegmentDocumentMapper.Map(s1), SegmentDocumentMapper.Map(s2), SegmentDocumentMapper.Map(s3) },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(query: null, sourceId: null, chapterNumber: 2, verseNumber: 3, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.Id.Should().Be(s2.Id);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyIndex_ReturnsEmptyPage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var service = NewService(client, descriptor);
        var result = await service.SearchAsync(query: "anything", sourceId: null, chapterNumber: null, verseNumber: null, page: 1, pageSize: 10, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }

    private static async Task<(MeilisearchClient Client, IsolatedDescriptor Descriptor, string IndexName)> SetUpIsolatedIndexAsync(MeilisearchFixture fixture, CancellationToken ct)
    {
        var client = fixture.CreateClient();
        var indexName = $"translations-query-{Guid.NewGuid():N}";
        var descriptor = new IsolatedDescriptor(indexName);

        var createTask = await client.CreateIndexAsync(indexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);

        var settingsTask = await client.Index(indexName).UpdateSettingsAsync(
            SearchIndexInitializerHostedService.ToMeilisearchSettings(descriptor.Settings),
            ct);
        await client.WaitForTaskAsync(settingsTask.TaskUid, cancellationToken: ct);

        return (client, descriptor, indexName);
    }

    private static SegmentSearchService NewService(MeilisearchClient client, IsolatedDescriptor descriptor)
    {
        var query = new MeilisearchSearchIndexQuery<SegmentSearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndexQuery<SegmentSearchDocument>>.Instance);
        return new SegmentSearchService(query, NullLogger<SegmentSearchService>.Instance);
    }

    private static async Task<Shared.Pagination.PagedResult<SegmentSearchHitDto>> WaitForHitsAsync(
        Func<Task<Shared.Results.Result<Shared.Pagination.PagedResult<SegmentSearchHitDto>>>> search,
        int expected,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        Shared.Pagination.PagedResult<SegmentSearchHitDto>? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = await search();
            result.IsSuccess.Should().BeTrue();
            last = result.Value!;
            if (last.Total >= expected)
            {
                return last;
            }

            await Task.Delay(150, ct);
        }

        throw new Xunit.Sdk.XunitException($"Expected at least {expected} hit(s), got {last?.Total ?? 0} within timeout.");
    }

    private sealed class IsolatedDescriptor : ISearchIndexDescriptor<SegmentSearchDocument>
    {
        private readonly SegmentIndexDescriptor inner = new();

        public IsolatedDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
