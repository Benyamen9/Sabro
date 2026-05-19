using Meilisearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Search;

namespace Sabro.IntegrationTests.Translations.Application.Search;

[Collection(TranslationsCollection.Name)]
public class SegmentSearchRebuilderTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public SegmentSearchRebuilderTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task RebuildAsync_IndexesLatestVersionOnly()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"translations-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedSegmentDescriptor(indexName);

        var (v1Id, v2Id) = await SeedSegmentChainAsync(ct);
        var rebuilder = NewRebuilder(client, descriptor);

        var result = await rebuilder.RebuildAsync(ct);

        result.DocumentCount.Should().BeGreaterThanOrEqualTo(1);
        var v2 = await WaitForDocumentAsync(client, indexName, v2Id.ToString("D"), ct);
        v2.Should().NotBeNull();
        v2!.Version.Should().Be(2);

        await Task.Delay(300, ct);
        var v1 = await TryGetDocumentAsync(client, indexName, v1Id.ToString("D"), ct);
        v1.Should().BeNull("only the latest segment version should land in the rebuilt index");
    }

    [Fact]
    public async Task RebuildAsync_WipesStaleDocuments()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"translations-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedSegmentDescriptor(indexName);

        var staleId = Guid.NewGuid().ToString("D");
        await EnsureIndexAsync(client, descriptor, ct);
        var addTask = await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                new SegmentSearchDocument
                {
                    Id = staleId,
                    SourceId = Guid.NewGuid().ToString("D"),
                    ChapterNumber = 1,
                    VerseNumber = 1,
                    TextVersionId = Guid.NewGuid().ToString("D"),
                    Content = "stale content",
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
        IsolatedSegmentDescriptor descriptor,
        CancellationToken ct)
    {
        var createTask = await client.CreateIndexAsync(descriptor.IndexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);
    }

    private static async Task<SegmentSearchDocument?> WaitForDocumentAsync(
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
                return await client.Index(indexName).GetDocumentAsync<SegmentSearchDocument>(documentId, cancellationToken: ct);
            }
            catch (MeilisearchApiError)
            {
                await Task.Delay(150, ct);
            }
        }

        return null;
    }

    private static async Task<SegmentSearchDocument?> TryGetDocumentAsync(
        MeilisearchClient client,
        string indexName,
        string documentId,
        CancellationToken ct)
    {
        try
        {
            return await client.Index(indexName).GetDocumentAsync<SegmentSearchDocument>(documentId, cancellationToken: ct);
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
                await client.Index(indexName).GetDocumentAsync<SegmentSearchDocument>(documentId, cancellationToken: ct);
                await Task.Delay(150, ct);
            }
            catch (MeilisearchApiError)
            {
                return;
            }
        }

        throw new Xunit.Sdk.XunitException($"Document {documentId} was not deleted from index {indexName} within timeout.");
    }

    private SegmentSearchRebuilder NewRebuilder(
        MeilisearchClient client,
        IsolatedSegmentDescriptor descriptor)
    {
        var searchIndex = new MeilisearchSearchIndex<SegmentSearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndex<SegmentSearchDocument>>.Instance);
        var ctx = postgres.CreateContext();
        return new SegmentSearchRebuilder(
            ctx,
            searchIndex,
            descriptor,
            NullLogger<SegmentSearchRebuilder>.Instance);
    }

    private async Task<(Guid V1Id, Guid V2Id)> SeedSegmentChainAsync(CancellationToken ct)
    {
        var seed = await postgres.SeedSegmentAsync(chapter: 1, verse: 1, ct, content: "first draft");

        await using var ctx = postgres.CreateContext();
        var v1 = await ctx.Segments.AsNoTracking().FirstAsync(s => s.Id == seed.SegmentId, ct);
        var v2 = v1.CreateNextVersion("second draft").Value!;
        ctx.Segments.Add(v2);
        await ctx.SaveChangesAsync(ct);

        return (v1.Id, v2.Id);
    }

    private sealed class IsolatedSegmentDescriptor : ISearchIndexDescriptor<SegmentSearchDocument>
    {
        private readonly SegmentIndexDescriptor inner = new();

        public IsolatedSegmentDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
