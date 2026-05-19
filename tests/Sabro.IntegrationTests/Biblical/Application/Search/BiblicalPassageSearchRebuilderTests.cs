using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Biblical.Application.Search;

[Collection(TranslationsCollection.Name)]
public class BiblicalPassageSearchRebuilderTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public BiblicalPassageSearchRebuilderTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task RebuildAsync_DenormalizesBookOntoPassageDocuments()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"biblical-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedBiblicalDescriptor(indexName);

        var (passageId, bookCode, englishName) = await SeedPassageAsync(chapter: 3, verse: 16, ct);
        var rebuilder = NewRebuilder(client, descriptor);

        var result = await rebuilder.RebuildAsync(ct);

        result.DocumentCount.Should().BeGreaterThanOrEqualTo(1);
        var doc = await WaitForDocumentAsync(client, indexName, passageId.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.BookCode.Should().Be(bookCode);
        doc.BookEnglishName.Should().Be(englishName);
        doc.ChapterNumber.Should().Be(3);
        doc.VerseNumber.Should().Be(16);
        doc.Reference.Should().Be($"{englishName} 3:16");
    }

    [Fact]
    public async Task RebuildAsync_WipesStaleDocuments()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"biblical-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedBiblicalDescriptor(indexName);

        var staleId = Guid.NewGuid().ToString("D");
        await EnsureIndexAsync(client, descriptor, ct);
        var addTask = await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                new BiblicalPassageSearchDocument
                {
                    Id = staleId,
                    BookId = Guid.NewGuid().ToString("D"),
                    BookCode = "OLD",
                    BookEnglishName = "Stale",
                    BookSyriacName = null,
                    Testament = "Old",
                    BookOrder = 1,
                    ChapterNumber = 1,
                    VerseNumber = 1,
                    Reference = "Stale 1:1",
                },
            },
            descriptor.PrimaryKey,
            ct);
        await client.WaitForTaskAsync(addTask.TaskUid, cancellationToken: ct);

        var rebuilder = NewRebuilder(client, descriptor);
        await rebuilder.RebuildAsync(ct);

        await WaitForDocumentDeletedAsync(client, indexName, staleId, ct);
    }

    private static string RandomBookCode()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var rng = Random.Shared;
        Span<char> buffer = stackalloc char[3];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = letters[rng.Next(letters.Length)];
        }

        return new string(buffer);
    }

    private static async Task EnsureIndexAsync(
        MeilisearchClient client,
        IsolatedBiblicalDescriptor descriptor,
        CancellationToken ct)
    {
        var createTask = await client.CreateIndexAsync(descriptor.IndexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);
    }

    private static async Task<BiblicalPassageSearchDocument?> WaitForDocumentAsync(
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
                return await client.Index(indexName).GetDocumentAsync<BiblicalPassageSearchDocument>(documentId, cancellationToken: ct);
            }
            catch (MeilisearchApiError)
            {
                await Task.Delay(150, ct);
            }
        }

        return null;
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
                await client.Index(indexName).GetDocumentAsync<BiblicalPassageSearchDocument>(documentId, cancellationToken: ct);
                await Task.Delay(150, ct);
            }
            catch (MeilisearchApiError)
            {
                return;
            }
        }

        throw new Xunit.Sdk.XunitException($"Document {documentId} was not deleted from index {indexName} within timeout.");
    }

    private BiblicalPassageSearchRebuilder NewRebuilder(
        MeilisearchClient client,
        IsolatedBiblicalDescriptor descriptor)
    {
        var searchIndex = new MeilisearchSearchIndex<BiblicalPassageSearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndex<BiblicalPassageSearchDocument>>.Instance);
        var ctx = postgres.CreateBiblicalContext();
        return new BiblicalPassageSearchRebuilder(
            ctx,
            searchIndex,
            descriptor,
            NullLogger<BiblicalPassageSearchRebuilder>.Instance);
    }

    private async Task<(Guid PassageId, string BookCode, string EnglishName)> SeedPassageAsync(int chapter, int verse, CancellationToken ct)
    {
        var code = RandomBookCode();
        var englishName = $"Book-{Guid.NewGuid():N}".Substring(0, 12);

        var book = BiblicalBook.Create(code, englishName, Testament.New, order: 1).Value!;
        var passage = BiblicalPassage.Create(book.Id, chapter, verse).Value!;

        await using var ctx = postgres.CreateBiblicalContext();
        ctx.Books.Add(book);
        ctx.Passages.Add(passage);
        await ctx.SaveChangesAsync(ct);

        return (passage.Id, book.Code, book.EnglishName);
    }

    private sealed class IsolatedBiblicalDescriptor : ISearchIndexDescriptor<BiblicalPassageSearchDocument>
    {
        private readonly BiblicalPassageIndexDescriptor inner = new();

        public IsolatedBiblicalDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
