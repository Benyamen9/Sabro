using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;
using Sabro.Translations.Application.Annotations;
using Sabro.Translations.Application.Search;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;

namespace Sabro.IntegrationTests.Translations.Application.Search;

[Collection(TranslationsCollection.Name)]
public class AnnotationSearchSyncTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public AnnotationSearchSyncTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task CreateAsync_PersistsDocumentToMeilisearch()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new AnnotationIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        var (segmentId, sourceId) = await SeedSegmentAsync(chapter: 4, verse: 9, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);

        var result = await service.CreateAsync(
            new CreateAnnotationRequest(segmentId, AnchorStart: 2, AnchorEnd: 7, Body: "Footnote on the term."),
            ct);

        result.IsSuccess.Should().BeTrue();
        var doc = await WaitForDocumentAsync(client, descriptor.IndexName, result.Value!.Id.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.SegmentId.Should().Be(segmentId.ToString("D"));
        doc.SourceId.Should().Be(sourceId.ToString("D"));
        doc.ChapterNumber.Should().Be(4);
        doc.VerseNumber.Should().Be(9);
        doc.Body.Should().Be("Footnote on the term.");
        doc.Version.Should().Be(1);
    }

    [Fact]
    public async Task EditAsync_DeletesPreviousVersionAndUpsertsNew()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new AnnotationIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        var (segmentId, _) = await SeedSegmentAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);

        var created = await service.CreateAsync(
            new CreateAnnotationRequest(segmentId, 0, 5, "first body"),
            ct);
        created.IsSuccess.Should().BeTrue();
        var v1Id = created.Value!.Id;
        await WaitForDocumentAsync(client, descriptor.IndexName, v1Id.ToString("D"), ct);

        var edited = await service.EditAsync(new EditAnnotationRequest(v1Id, "second body"), ct);
        edited.IsSuccess.Should().BeTrue();
        var v2Id = edited.Value!.Id;

        var v2Doc = await WaitForDocumentAsync(client, descriptor.IndexName, v2Id.ToString("D"), ct);
        v2Doc.Should().NotBeNull();
        v2Doc!.Body.Should().Be("second body");
        v2Doc.Version.Should().Be(2);

        await WaitForDocumentDeletedAsync(client, descriptor.IndexName, v1Id.ToString("D"), ct);
    }

    [Fact]
    public async Task CreateAsync_NewVersionDocStartsWithNullApprovalStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new AnnotationIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        var (segmentId, _) = await SeedSegmentAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);

        var result = await service.CreateAsync(
            new CreateAnnotationRequest(segmentId, AnchorStart: 0, AnchorEnd: 5, Body: "fresh note"),
            ct);

        result.IsSuccess.Should().BeTrue();
        var doc = await WaitForDocumentAsync(client, descriptor.IndexName, result.Value!.Id.ToString("D"), ct);
        doc!.ApprovalStatus.Should().BeNull();
    }

    [Theory]
    [InlineData(AnnotationApprovalStatus.Approved, "approved")]
    [InlineData(AnnotationApprovalStatus.Rejected, "rejected")]
    public async Task UpdateApprovalStatusAsync_ReUpsertsDocWithApprovalStatus(AnnotationApprovalStatus status, string expectedString)
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new AnnotationIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        var (segmentId, _) = await SeedSegmentAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);

        var created = await service.CreateAsync(
            new CreateAnnotationRequest(segmentId, AnchorStart: 0, AnchorEnd: 5, Body: "body"),
            ct);
        created.IsSuccess.Should().BeTrue();
        var annotationId = created.Value!.Id;

        await ((IAnnotationApprovalIndexer)service).UpdateApprovalStatusAsync(annotationId, status, ct);

        var doc = await WaitForDocumentApprovalStatusAsync(client, descriptor.IndexName, annotationId.ToString("D"), expectedString, ct);
        doc.Should().NotBeNull();
        doc!.ApprovalStatus.Should().Be(expectedString);
    }

    [Fact]
    public async Task UpdateApprovalStatusAsync_OnUnknownAnnotation_IsNoOp()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new AnnotationIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);

        var act = async () => await ((IAnnotationApprovalIndexer)service)
            .UpdateApprovalStatusAsync(Guid.NewGuid(), AnnotationApprovalStatus.Approved, ct);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EditAsync_ResetsApprovalStatusOnNewVersion()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new AnnotationIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        var (segmentId, _) = await SeedSegmentAsync(chapter: 1, verse: 1, ct);

        await using var ctx = postgres.CreateContext();
        var service = NewService(ctx, searchIndex);

        var created = await service.CreateAsync(
            new CreateAnnotationRequest(segmentId, AnchorStart: 0, AnchorEnd: 5, Body: "v1 body"),
            ct);
        created.IsSuccess.Should().BeTrue();
        var v1Id = created.Value!.Id;

        await ((IAnnotationApprovalIndexer)service)
            .UpdateApprovalStatusAsync(v1Id, AnnotationApprovalStatus.Approved, ct);
        await WaitForDocumentApprovalStatusAsync(client, descriptor.IndexName, v1Id.ToString("D"), "approved", ct);

        var edited = await service.EditAsync(new EditAnnotationRequest(v1Id, "v2 body"), ct);
        edited.IsSuccess.Should().BeTrue();
        var v2Id = edited.Value!.Id;

        var v2Doc = await WaitForDocumentAsync(client, descriptor.IndexName, v2Id.ToString("D"), ct);
        v2Doc!.ApprovalStatus.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_WhenMeilisearchUnreachable_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var brokenClient = new MeilisearchClient("http://127.0.0.1:1", apiKey: null);
        var searchIndex = NewSearchIndex(brokenClient, new AnnotationIndexDescriptor());
        var segment = Segment.Create(Guid.NewGuid(), 1, 1, Guid.NewGuid(), "noop").Value!;
        var annotation = Annotation.Create(segment.Id, 0, 5, "noop body").Value!;
        var doc = AnnotationDocumentMapper.Map(annotation, segment);

        var act = async () => await searchIndex.UpsertAsync(doc, ct);

        await act.Should().NotThrowAsync();
    }

    private static MeilisearchSearchIndex<AnnotationSearchDocument> NewSearchIndex(
        MeilisearchClient client, AnnotationIndexDescriptor descriptor) =>
        new(client, descriptor, NullLogger<MeilisearchSearchIndex<AnnotationSearchDocument>>.Instance);

    private static AnnotationService NewService(
        TranslationsDbContext ctx, ISearchIndex<AnnotationSearchDocument> searchIndex) =>
        new(
            ctx,
            new CreateAnnotationRequestValidator(),
            new EditAnnotationRequestValidator(),
            searchIndex,
            NullLogger<AnnotationService>.Instance);

    private static async Task EnsureIndexAsync(
        MeilisearchClient client, AnnotationIndexDescriptor descriptor, CancellationToken ct)
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

    private static async Task<AnnotationSearchDocument?> WaitForDocumentAsync(
        MeilisearchClient client, string indexName, string documentId, CancellationToken ct)
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

    private static async Task<AnnotationSearchDocument?> WaitForDocumentApprovalStatusAsync(
        MeilisearchClient client, string indexName, string documentId, string expectedStatus, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        AnnotationSearchDocument? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                last = await client.Index(indexName).GetDocumentAsync<AnnotationSearchDocument>(documentId, cancellationToken: ct);
                if (last.ApprovalStatus == expectedStatus)
                {
                    return last;
                }
            }
            catch (MeilisearchApiError)
            {
            }

            await Task.Delay(150, ct);
        }

        throw new Xunit.Sdk.XunitException(
            $"Expected document {documentId} in index {indexName} to have approvalStatus '{expectedStatus}', got '{last?.ApprovalStatus}' within timeout.");
    }

    private static async Task WaitForDocumentDeletedAsync(
        MeilisearchClient client, string indexName, string documentId, CancellationToken ct)
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

    private static string RandomLetterCode() =>
        new(new[]
        {
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
        });

    private async Task<(Guid SegmentId, Guid SourceId)> SeedSegmentAsync(int chapter, int verse, CancellationToken ct)
    {
        var author = Author.Create($"Author-{Guid.NewGuid():N}").Value!;
        var source = Source.Create(author.Id, $"Source-{Guid.NewGuid():N}").Value!;
        var textVersion = TextVersion.Create(RandomLetterCode(), $"Tv-{Guid.NewGuid():N}", isRightToLeft: false).Value!;
        var segment = Segment.Create(source.Id, chapter, verse, textVersion.Id, "Some content.").Value!;

        await using var ctx = postgres.CreateContext();
        ctx.Authors.Add(author);
        ctx.Sources.Add(source);
        ctx.TextVersions.Add(textVersion);
        ctx.Segments.Add(segment);
        await ctx.SaveChangesAsync(ct);

        return (segment.Id, source.Id);
    }
}
