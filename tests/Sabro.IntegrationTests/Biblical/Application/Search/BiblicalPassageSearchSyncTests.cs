using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Biblical.Application.Books;
using Sabro.Biblical.Application.Passages;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;
using Sabro.Biblical.Infrastructure;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Biblical.Application.Search;

[Collection(IntegrationCollection.Name)]
public class BiblicalPassageSearchSyncTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public BiblicalPassageSearchSyncTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task GetOrCreateAsync_OnFirstCall_PersistsDocumentToMeilisearch()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new BiblicalPassageIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);
        var searchIndex = NewSearchIndex(client, descriptor);

        var code = await SeedBookAsync("Matthew", Testament.New, syriacName: "ܡܬܝ", ct: ct);

        await using var ctx = postgres.CreateBiblicalContext();
        var service = NewService(ctx, searchIndex);

        var result = await service.GetOrCreateAsync(
            new GetOrCreateBiblicalPassageRequest(code, 3, 16),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasCreated.Should().BeTrue();

        var doc = await WaitForDocumentAsync(client, descriptor.IndexName, result.Value.Passage.Id.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.BookCode.Should().Be(code);
        doc.BookEnglishName.Should().Be("Matthew");
        doc.BookSyriacName.Should().Be("ܡܬܝ");
        doc.Testament.Should().Be("New");
        doc.ChapterNumber.Should().Be(3);
        doc.VerseNumber.Should().Be(16);
        doc.Reference.Should().Be("Matthew 3:16");
    }

    [Fact]
    public async Task UpsertAsync_WhenMeilisearchUnreachable_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var brokenClient = new MeilisearchClient("http://127.0.0.1:1", apiKey: null);
        var searchIndex = NewSearchIndex(brokenClient, new BiblicalPassageIndexDescriptor());
        var book = BiblicalBook.Create("MAT", "Matthew", Testament.New, 40).Value!;
        var passage = BiblicalPassage.Create(book.Id, 1, 1).Value!;
        var doc = BiblicalPassageDocumentMapper.Map(passage, book);

        var act = async () => await searchIndex.UpsertAsync(doc, ct);

        await act.Should().NotThrowAsync();
    }

    private static MeilisearchSearchIndex<BiblicalPassageSearchDocument> NewSearchIndex(
        MeilisearchClient client, BiblicalPassageIndexDescriptor descriptor) =>
        new(client, descriptor, new MeilisearchOptions(), NullLogger<MeilisearchSearchIndex<BiblicalPassageSearchDocument>>.Instance);

    private static BiblicalPassageService NewService(
        BiblicalDbContext ctx, ISearchIndex<BiblicalPassageSearchDocument> searchIndex) =>
        new(
            ctx,
            new GetOrCreateBiblicalPassageRequestValidator(),
            searchIndex,
            NullLogger<BiblicalPassageService>.Instance);

    private static async Task EnsureIndexAsync(
        MeilisearchClient client, BiblicalPassageIndexDescriptor descriptor, CancellationToken ct)
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

    private static async Task<BiblicalPassageSearchDocument?> WaitForDocumentAsync(
        MeilisearchClient client, string indexName, string documentId, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                return await client.Index(indexName).GetDocumentAsync<BiblicalPassageSearchDocument>(documentId, cancellationToken: ct);
            }
            catch (MeilisearchApiError)
            {
                await Task.Delay(150, ct);
            }
        }

        return null;
    }

    private static string NewCode()
    {
        var n = Random.Shared.Next(0, 1_000_000);
        return $"S{n:D6}";
    }

    private async Task<string> SeedBookAsync(string englishName, Testament testament, string? syriacName, CancellationToken ct)
    {
        var code = NewCode();
        await using var ctx = postgres.CreateBiblicalContext();
        var service = new BiblicalBookService(
            ctx,
            new CreateBiblicalBookRequestValidator(),
            NullLogger<BiblicalBookService>.Instance);
        var result = await service.CreateAsync(
            new CreateBiblicalBookRequest(code, englishName, testament, Random.Shared.Next(1, 200), syriacName),
            ct);
        result.IsSuccess.Should().BeTrue();
        return code;
    }
}
