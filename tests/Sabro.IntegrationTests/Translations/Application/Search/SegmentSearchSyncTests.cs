using Meilisearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Search;
using Sabro.Translations.Application.Segments;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;

namespace Sabro.IntegrationTests.Translations.Application.Search;

[Collection(TranslationsCollection.Name)]
public class SegmentSearchSyncTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public SegmentSearchSyncTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task CreateAsync_PersistsDocumentToMeilisearch()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new SegmentIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        await using var seedCtx = postgres.CreateContext();
        var (sourceId, textVersionId) = await SeedAsync(seedCtx, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);
        var result = await service.CreateAsync(
            new CreateSegmentRequest(sourceId, ChapterNumber: 1, VerseNumber: 1, textVersionId, Content: "In principio"),
            ct);

        result.IsSuccess.Should().BeTrue();
        var doc = await WaitForDocumentAsync(client, descriptor.IndexName, result.Value!.Id.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.Content.Should().Be("In principio");
        doc.Version.Should().Be(1);
        doc.SourceId.Should().Be(sourceId.ToString("D"));
    }

    [Fact]
    public async Task EditAsync_DeletesPreviousVersionAndUpsertsNew()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new SegmentIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        await using var seedCtx = postgres.CreateContext();
        var (sourceId, textVersionId) = await SeedAsync(seedCtx, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);
        var created = await service.CreateAsync(
            new CreateSegmentRequest(sourceId, 2, 3, textVersionId, "version one"),
            ct);
        created.IsSuccess.Should().BeTrue();
        var v1Id = created.Value!.Id;

        await WaitForDocumentAsync(client, descriptor.IndexName, v1Id.ToString("D"), ct);

        var edited = await service.EditAsync(new EditSegmentRequest(v1Id, "version two"), ct);
        edited.IsSuccess.Should().BeTrue();
        var v2Id = edited.Value!.Id;

        var v2Doc = await WaitForDocumentAsync(client, descriptor.IndexName, v2Id.ToString("D"), ct);
        v2Doc.Should().NotBeNull();
        v2Doc!.Content.Should().Be("version two");
        v2Doc.Version.Should().Be(2);

        await WaitForDocumentDeletedAsync(client, descriptor.IndexName, v1Id.ToString("D"), ct);
    }

    [Fact]
    public async Task UpsertAsync_WhenMeilisearchUnreachable_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var brokenClient = new MeilisearchClient("http://127.0.0.1:1", apiKey: null);
        var searchIndex = NewSearchIndex(brokenClient, new SegmentIndexDescriptor());
        var segment = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "noop").Value!;
        var doc = SegmentDocumentMapper.Map(segment);

        var act = async () => await searchIndex.UpsertAsync(doc, ct);

        await act.Should().NotThrowAsync();
    }

    private static MeilisearchSearchIndex<SegmentSearchDocument> NewSearchIndex(
        MeilisearchClient client, SegmentIndexDescriptor descriptor) =>
        new(client, descriptor, NullLogger<MeilisearchSearchIndex<SegmentSearchDocument>>.Instance);

    private static SegmentService NewService(
        TranslationsDbContext ctx, ISearchIndex<SegmentSearchDocument> searchIndex) =>
        new(
            ctx,
            new CreateSegmentRequestValidator(),
            new EditSegmentRequestValidator(),
            searchIndex,
            NullLogger<SegmentService>.Instance);

    private static async Task EnsureIndexAsync(
        MeilisearchClient client, SegmentIndexDescriptor descriptor, CancellationToken ct)
    {
        try
        {
            await client.GetIndexAsync(descriptor.IndexName, ct);
        }
        catch (MeilisearchApiError)
        {
            await client.CreateIndexAsync(descriptor.IndexName, descriptor.PrimaryKey, ct);
        }
    }

    private static async Task<(Guid SourceId, Guid TextVersionId)> SeedAsync(
        TranslationsDbContext ctx, CancellationToken ct)
    {
        var author = Author.Create("Dionysios bar Ṣalibi").Value!;
        ctx.Authors.Add(author);
        await ctx.SaveChangesAsync(ct);

        var source = Source.Create(author.Id, "Commentary on Matthew").Value!;
        ctx.Sources.Add(source);
        await ctx.SaveChangesAsync(ct);

        var version = TextVersion.Create(RandomLetterCode(), "Initial version", isRightToLeft: false).Value!;
        ctx.TextVersions.Add(version);
        await ctx.SaveChangesAsync(ct);

        return (source.Id, version.Id);
    }

    private static string RandomLetterCode() =>
        new(new[]
        {
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
        });

    private static async Task<SegmentSearchDocument?> WaitForDocumentAsync(
        MeilisearchClient client, string indexName, string documentId, CancellationToken ct)
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

    private static async Task WaitForDocumentDeletedAsync(
        MeilisearchClient client, string indexName, string documentId, CancellationToken ct)
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
}
