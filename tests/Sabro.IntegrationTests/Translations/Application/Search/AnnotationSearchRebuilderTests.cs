using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Search;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Translations.Application.Search;

[Collection(TranslationsCollection.Name)]
public class AnnotationSearchRebuilderTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public AnnotationSearchRebuilderTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task RebuildAsync_IndexesLatestVersionWithDenormalizedParent()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"annotations-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedAnnotationDescriptor(indexName);

        var seed = await postgres.SeedAnnotationAsync(chapter: 5, verse: 3, ct);
        var rebuilder = NewRebuilder(client, descriptor);

        var result = await rebuilder.RebuildAsync(ct);

        result.DocumentCount.Should().BeGreaterThanOrEqualTo(1);
        var doc = await WaitForDocumentAsync(client, indexName, seed.AnnotationId.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.SegmentId.Should().Be(seed.SegmentId.ToString("D"));
        doc.SourceId.Should().Be(seed.SourceId.ToString("D"));
        doc.ChapterNumber.Should().Be(5);
        doc.VerseNumber.Should().Be(3);
        doc.ApprovalStatus.Should().BeNull(
            "approval status is owned by Reviews and is restored by the republish step, not by the rebuilder");
    }

    [Fact]
    public async Task RebuildAsync_DropsSupersededAnnotationVersions()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"annotations-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedAnnotationDescriptor(indexName);

        var (v1Id, v2Id) = await SeedAnnotationChainAsync(ct);
        var rebuilder = NewRebuilder(client, descriptor);

        await rebuilder.RebuildAsync(ct);

        var v2 = await WaitForDocumentAsync(client, indexName, v2Id.ToString("D"), ct);
        v2.Should().NotBeNull();
        v2!.Version.Should().Be(2);

        await Task.Delay(300, ct);
        var v1 = await TryGetDocumentAsync(client, indexName, v1Id.ToString("D"), ct);
        v1.Should().BeNull("only the latest annotation version should land in the rebuilt index");
    }

    [Fact]
    public async Task RebuildAsync_WipesStaleDocuments()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"annotations-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedAnnotationDescriptor(indexName);

        var staleId = Guid.NewGuid().ToString("D");
        await EnsureIndexAsync(client, descriptor, ct);
        var addTask = await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                new AnnotationSearchDocument
                {
                    Id = staleId,
                    SegmentId = Guid.NewGuid().ToString("D"),
                    SourceId = Guid.NewGuid().ToString("D"),
                    ChapterNumber = 1,
                    VerseNumber = 1,
                    AnchorStart = 0,
                    AnchorEnd = 1,
                    Body = "stale",
                    Version = 1,
                    CreatedAtUnix = 0,
                },
            },
            descriptor.PrimaryKey,
            ct);
        await client.WaitForTaskAsync(addTask.TaskUid, cancellationToken: ct);

        var rebuilder = NewRebuilder(client, descriptor);
        await rebuilder.RebuildAsync(ct);

        await WaitForDocumentDeletedAsync(client, indexName, staleId, ct);
    }

    private static async Task EnsureIndexAsync(
        MeilisearchClient client,
        IsolatedAnnotationDescriptor descriptor,
        CancellationToken ct)
    {
        var createTask = await client.CreateIndexAsync(descriptor.IndexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);
    }

    private static async Task<AnnotationSearchDocument?> WaitForDocumentAsync(
        MeilisearchClient client,
        string indexName,
        string documentId,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                return await client.Index(indexName).GetDocumentAsync<AnnotationSearchDocument>(documentId, cancellationToken: ct);
            }
            catch (MeilisearchApiError)
            {
                await Task.Delay(150, ct);
            }
        }

        return null;
    }

    private static async Task<AnnotationSearchDocument?> TryGetDocumentAsync(
        MeilisearchClient client,
        string indexName,
        string documentId,
        CancellationToken ct)
    {
        try
        {
            return await client.Index(indexName).GetDocumentAsync<AnnotationSearchDocument>(documentId, cancellationToken: ct);
        }
        catch (MeilisearchApiError)
        {
            return null;
        }
    }

    private static async Task WaitForDocumentDeletedAsync(
        MeilisearchClient client,
        string indexName,
        string documentId,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                await client.Index(indexName).GetDocumentAsync<AnnotationSearchDocument>(documentId, cancellationToken: ct);
                await Task.Delay(150, ct);
            }
            catch (MeilisearchApiError)
            {
                return;
            }
        }

        throw new Xunit.Sdk.XunitException($"Document {documentId} was not deleted from index {indexName} within timeout.");
    }

    private AnnotationSearchRebuilder NewRebuilder(
        MeilisearchClient client,
        IsolatedAnnotationDescriptor descriptor)
    {
        var searchIndex = new MeilisearchSearchIndex<AnnotationSearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndex<AnnotationSearchDocument>>.Instance);
        var ctx = postgres.CreateContext();
        return new AnnotationSearchRebuilder(
            ctx,
            searchIndex,
            descriptor,
            NullLogger<AnnotationSearchRebuilder>.Instance);
    }

    private async Task<(Guid V1Id, Guid V2Id)> SeedAnnotationChainAsync(CancellationToken ct)
    {
        var segmentSeed = await postgres.SeedSegmentAsync(chapter: 1, verse: 1, ct);
        var v1 = Annotation.Create(segmentSeed.SegmentId, 0, 5, "v1 body").Value!;
        var v2 = v1.CreateNextVersion("v2 body").Value!;

        await using var ctx = postgres.CreateContext();
        ctx.Annotations.AddRange(v1, v2);
        await ctx.SaveChangesAsync(ct);

        return (v1.Id, v2.Id);
    }

    private sealed class IsolatedAnnotationDescriptor : ISearchIndexDescriptor<AnnotationSearchDocument>
    {
        private readonly AnnotationIndexDescriptor inner = new();

        public IsolatedAnnotationDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
