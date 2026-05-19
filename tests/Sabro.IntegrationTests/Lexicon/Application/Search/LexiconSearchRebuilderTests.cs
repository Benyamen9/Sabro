using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Lexicon.Application.Search;

[Collection(TranslationsCollection.Name)]
public class LexiconSearchRebuilderTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public LexiconSearchRebuilderTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task RebuildAsync_PopulatesIndexFromPostgres()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"lexicon-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedLexiconDescriptor(indexName);

        var entryIds = await SeedEntriesAsync(count: 3, ct);
        var rebuilder = NewRebuilder(client, descriptor);

        var result = await rebuilder.RebuildAsync(ct);

        result.DocumentCount.Should().BeGreaterThanOrEqualTo(3);
        foreach (var id in entryIds)
        {
            var doc = await WaitForDocumentAsync(client, indexName, id.ToString("D"), ct);
            doc.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task RebuildAsync_WipesStaleDocuments()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"lexicon-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedLexiconDescriptor(indexName);

        var staleId = Guid.NewGuid().ToString("D");
        await EnsureIndexAsync(client, descriptor, ct);
        var addTask = await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                new LexiconEntrySearchDocument
                {
                    Id = staleId,
                    SyriacUnvocalized = "ܙܒܢ",
                    SblTransliteration = "zbn",
                    GrammaticalCategory = "Verb",
                    CreatedAtUnix = 0,
                },
            },
            descriptor.PrimaryKey,
            ct);
        await client.WaitForTaskAsync(addTask.TaskUid, cancellationToken: ct);

        await SeedEntriesAsync(count: 1, ct);
        var rebuilder = NewRebuilder(client, descriptor);

        await rebuilder.RebuildAsync(ct);

        await WaitForDocumentDeletedAsync(client, indexName, staleId, ct);
    }

    [Fact]
    public async Task RebuildAsync_DenormalizesRootForm()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var indexName = $"lexicon-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedLexiconDescriptor(indexName);

        var rootForm = RandomSyriacWord(4);
        Guid rootId;
        Guid entryId;
        await using (var seedCtx = postgres.CreateLexiconContext())
        {
            var root = LexiconRoot.Create(rootForm).Value!;
            seedCtx.Roots.Add(root);

            var entry = LexiconEntry.Create(
                RandomSyriacWord(4),
                $"sbl-{Guid.NewGuid():N}".Substring(0, 12),
                GrammaticalCategory.Noun,
                rootId: root.Id).Value!;
            seedCtx.Entries.Add(entry);
            await seedCtx.SaveChangesAsync(ct);

            rootId = root.Id;
            entryId = entry.Id;
        }

        var rebuilder = NewRebuilder(client, descriptor);
        var result = await rebuilder.RebuildAsync(ct);

        result.DocumentCount.Should().BeGreaterThanOrEqualTo(1);
        var doc = await WaitForDocumentAsync(client, indexName, entryId.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.RootId.Should().Be(rootId.ToString("D"));
        doc.RootForm.Should().Be(rootForm);
    }

    [Fact]
    public async Task RebuildAsync_OnEmptyDatabase_ReturnsZeroAndConfiguresIndex()
    {
        var ct = TestContext.Current.CancellationToken;

        await ClearLexiconEntriesAsync(ct);

        var client = meili.CreateClient();
        var indexName = $"lexicon-rebuild-{Guid.NewGuid():N}";
        var descriptor = new IsolatedLexiconDescriptor(indexName);
        var rebuilder = NewRebuilder(client, descriptor);

        var result = await rebuilder.RebuildAsync(ct);

        result.DocumentCount.Should().Be(0);
        await WaitForFilterableAttributeAsync(client, indexName, "grammaticalCategory", ct);
    }

    private static async Task EnsureIndexAsync(
        MeilisearchClient client,
        IsolatedLexiconDescriptor descriptor,
        CancellationToken ct)
    {
        var createTask = await client.CreateIndexAsync(descriptor.IndexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);
    }

    private static async Task<LexiconEntrySearchDocument?> WaitForDocumentAsync(
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
                return await client.Index(indexName).GetDocumentAsync<LexiconEntrySearchDocument>(documentId, cancellationToken: ct);
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
                await client.Index(indexName).GetDocumentAsync<LexiconEntrySearchDocument>(documentId, cancellationToken: ct);
                await Task.Delay(150, ct);
            }
            catch (MeilisearchApiError)
            {
                return;
            }
        }

        throw new Xunit.Sdk.XunitException($"Document {documentId} was not deleted from index {indexName} within timeout.");
    }

    private static string RandomSyriacWord(int length)
    {
        const int rangeStart = 0x0710;
        const int rangeEnd = 0x072F;
        var rng = Random.Shared;
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = (char)rng.Next(rangeStart, rangeEnd + 1);
        }

        return new string(buffer);
    }

    private static async Task WaitForFilterableAttributeAsync(
        MeilisearchClient client,
        string indexName,
        string attribute,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var settings = await client.Index(indexName).GetSettingsAsync(ct);
                if (settings.FilterableAttributes is not null && settings.FilterableAttributes.Contains(attribute))
                {
                    return;
                }
            }
            catch (MeilisearchApiError)
            {
            }

            await Task.Delay(150, ct);
        }

        throw new Xunit.Sdk.XunitException(
            $"Expected index {indexName} to declare '{attribute}' filterable within timeout.");
    }

    private LexiconSearchRebuilder NewRebuilder(
        MeilisearchClient client,
        IsolatedLexiconDescriptor descriptor)
    {
        var searchIndex = new MeilisearchSearchIndex<LexiconEntrySearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndex<LexiconEntrySearchDocument>>.Instance);
        var ctx = postgres.CreateLexiconContext();
        return new LexiconSearchRebuilder(
            ctx,
            searchIndex,
            descriptor,
            NullLogger<LexiconSearchRebuilder>.Instance);
    }

    private async Task<List<Guid>> SeedEntriesAsync(int count, CancellationToken ct)
    {
        var ids = new List<Guid>(count);
        await using var ctx = postgres.CreateLexiconContext();
        for (var i = 0; i < count; i++)
        {
            var entry = LexiconEntry.Create(
                $"ܟܬܒ",
                $"ktb-{Guid.NewGuid():N}".Substring(0, 12),
                GrammaticalCategory.Verb).Value!;
            ctx.Entries.Add(entry);
            ids.Add(entry.Id);
        }

        await ctx.SaveChangesAsync(ct);
        return ids;
    }

    private async Task ClearLexiconEntriesAsync(CancellationToken ct)
    {
        await using var ctx = postgres.CreateLexiconContext();
        ctx.Entries.RemoveRange(ctx.Entries);
        ctx.Roots.RemoveRange(ctx.Roots);
        await ctx.SaveChangesAsync(ct);
    }

    private sealed class IsolatedLexiconDescriptor : ISearchIndexDescriptor<LexiconEntrySearchDocument>
    {
        private readonly LexiconEntryIndexDescriptor inner = new();

        public IsolatedLexiconDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
