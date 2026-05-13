using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Search;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Translations.Application.Search;

[Collection(TranslationsCollection.Name)]
public class AnnotationSearchQueryTests
{
    private readonly MeilisearchFixture meili;

    public AnnotationSearchQueryTests(MeilisearchFixture meili)
    {
        this.meili = meili;
    }

    [Fact]
    public async Task SearchAsync_OverFreeTextQuery_ReturnsIndexedAnnotation()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var segment = Segment.Create(Guid.NewGuid(), 3, 16, Guid.NewGuid(), "Content.").Value!;
        var annotation = Annotation.Create(segment.Id, 0, 5, "Note on the Greek term logos.").Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[] { AnnotationDocumentMapper.Map(annotation, segment) },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(
                query: "logos",
                segmentId: null,
                sourceId: null,
                chapterNumber: null,
                verseNumber: null,
                approvalStatus: null,
                page: 1,
                pageSize: 10,
                ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.Body.Should().Contain("logos");
    }

    [Fact]
    public async Task SearchAsync_WithSegmentIdFilter_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var segmentA = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "Content A.").Value!;
        var segmentB = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "Content B.").Value!;
        var a = Annotation.Create(segmentA.Id, 0, 5, "Note on A.").Value!;
        var b = Annotation.Create(segmentB.Id, 0, 5, "Note on B.").Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                AnnotationDocumentMapper.Map(a, segmentA),
                AnnotationDocumentMapper.Map(b, segmentB),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(
                query: null,
                segmentId: segmentA.Id,
                sourceId: null,
                chapterNumber: null,
                verseNumber: null,
                approvalStatus: null,
                page: 1,
                pageSize: 10,
                ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.SegmentId.Should().Be(segmentA.Id);
    }

    [Fact]
    public async Task SearchAsync_WithSourceAndChapterFilters_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var sourceId = Guid.NewGuid();
        var otherSource = Guid.NewGuid();
        var c1 = Segment.Create(sourceId, 1, 1, Guid.NewGuid(), "Content c1.").Value!;
        var c3 = Segment.Create(sourceId, 3, 1, Guid.NewGuid(), "Content c3.").Value!;
        var other = Segment.Create(otherSource, 3, 1, Guid.NewGuid(), "Content other.").Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                AnnotationDocumentMapper.Map(Annotation.Create(c1.Id, 0, 5, "note c1").Value!, c1),
                AnnotationDocumentMapper.Map(Annotation.Create(c3.Id, 0, 5, "note c3").Value!, c3),
                AnnotationDocumentMapper.Map(Annotation.Create(other.Id, 0, 5, "note other").Value!, other),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(
                query: null,
                segmentId: null,
                sourceId: sourceId,
                chapterNumber: 3,
                verseNumber: null,
                approvalStatus: null,
                page: 1,
                pageSize: 10,
                ct),
            expected: 1,
            ct);

        var hit = hits.Items.Should().ContainSingle().Subject;
        hit.SourceId.Should().Be(sourceId);
        hit.ChapterNumber.Should().Be(3);
    }

    [Fact]
    public async Task SearchAsync_WithApprovalStatusFilter_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var segment = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "Content.").Value!;
        var approved = Annotation.Create(segment.Id, 0, 5, "approved note").Value!;
        var rejected = Annotation.Create(segment.Id, 6, 10, "rejected note").Value!;
        var unreviewed = Annotation.Create(segment.Id, 11, 15, "unreviewed note").Value!;

        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                AnnotationDocumentMapper.Map(approved, segment, Sabro.Translations.Application.Annotations.AnnotationApprovalStatus.Approved),
                AnnotationDocumentMapper.Map(rejected, segment, Sabro.Translations.Application.Annotations.AnnotationApprovalStatus.Rejected),
                AnnotationDocumentMapper.Map(unreviewed, segment),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(
                query: null,
                segmentId: null,
                sourceId: null,
                chapterNumber: null,
                verseNumber: null,
                approvalStatus: "approved",
                page: 1,
                pageSize: 10,
                ct),
            expected: 1,
            ct);

        var hit = hits.Items.Should().ContainSingle().Subject;
        hit.Id.Should().Be(approved.Id);
        hit.ApprovalStatus.Should().Be("approved");
    }

    [Fact]
    public async Task SearchAsync_WithEmptyIndex_ReturnsEmptyPage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var service = NewService(client, descriptor);
        var result = await service.SearchAsync(
            query: "anything",
            segmentId: null,
            sourceId: null,
            chapterNumber: null,
            verseNumber: null,
            approvalStatus: null,
            page: 1,
            pageSize: 10,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }

    private static async Task<(MeilisearchClient Client, IsolatedDescriptor Descriptor, string IndexName)> SetUpIsolatedIndexAsync(MeilisearchFixture fixture, CancellationToken ct)
    {
        var client = fixture.CreateClient();
        var indexName = $"annotations-query-{Guid.NewGuid():N}";
        var descriptor = new IsolatedDescriptor(indexName);

        var createTask = await client.CreateIndexAsync(indexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);

        var settingsTask = await client.Index(indexName).UpdateSettingsAsync(
            SearchIndexInitializerHostedService.ToMeilisearchSettings(descriptor.Settings),
            ct);
        await client.WaitForTaskAsync(settingsTask.TaskUid, cancellationToken: ct);

        return (client, descriptor, indexName);
    }

    private static AnnotationSearchService NewService(
        MeilisearchClient client, IsolatedDescriptor descriptor)
    {
        var query = new MeilisearchSearchIndexQuery<AnnotationSearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndexQuery<AnnotationSearchDocument>>.Instance);
        return new AnnotationSearchService(query, NullLogger<AnnotationSearchService>.Instance);
    }

    private static async Task<Shared.Pagination.PagedResult<AnnotationSearchHitDto>> WaitForHitsAsync(
        Func<Task<Shared.Results.Result<Shared.Pagination.PagedResult<AnnotationSearchHitDto>>>> search,
        int expected,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        Shared.Pagination.PagedResult<AnnotationSearchHitDto>? last = null;
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

    /// <summary>
    /// Wraps the production <see cref="AnnotationIndexDescriptor"/> with a
    /// test-scoped index name so each test gets isolated documents.
    /// </summary>
    private sealed class IsolatedDescriptor : ISearchIndexDescriptor<AnnotationSearchDocument>
    {
        private readonly AnnotationIndexDescriptor inner = new();

        public IsolatedDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
