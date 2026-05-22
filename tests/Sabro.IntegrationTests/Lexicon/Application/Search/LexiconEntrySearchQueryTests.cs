using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Lexicon.Application.Search;

[Collection(IntegrationCollection.Name)]
public class LexiconEntrySearchQueryTests
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public LexiconEntrySearchQueryTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task SearchAsync_OverFreeTextQuery_ReturnsIndexedEntry()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var entry = LexiconEntry.Create("ܡܠܬܐ", "meltā", GrammaticalCategory.Noun).Value!;
        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);
        await client.Index(indexName).AddDocumentsAsync(new[] { doc }, descriptor.PrimaryKey, ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync("meltā", grammaticalCategory: null, rootId: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.Id.Should().Be(entry.Id);
        hits.Total.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_WithSynonyms_MatchesTransliterationVariants()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var entry = LexiconEntry.Create("ܡܠܬܐ", "meltā", GrammaticalCategory.Noun).Value!;
        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);
        await client.Index(indexName).AddDocumentsAsync(new[] { doc }, descriptor.PrimaryKey, ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync("meltho", grammaticalCategory: null, rootId: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.SblTransliteration.Should().Be("meltā");
    }

    [Fact]
    public async Task SearchAsync_WithCategoryFilter_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var verb = LexiconEntry.Create("ܟܬܒ", "ktb", GrammaticalCategory.Verb).Value!;
        var noun = LexiconEntry.Create("ܡܠܬܐ", "meltā", GrammaticalCategory.Noun).Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                LexiconEntryDocumentMapper.Map(verb, rootForm: null),
                LexiconEntryDocumentMapper.Map(noun, rootForm: null),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(query: null, grammaticalCategory: GrammaticalCategory.Noun, rootId: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.Id.Should().Be(noun.Id);
    }

    [Fact]
    public async Task SearchAsync_WithRootIdFilter_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var rootId = Guid.NewGuid();
        var rooted = LexiconEntry.Create("ܫܠܡܐ", "šlāmā", GrammaticalCategory.Noun, rootId: rootId).Value!;
        var unrooted = LexiconEntry.Create("ܡܠܬܐ", "meltā", GrammaticalCategory.Noun).Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                LexiconEntryDocumentMapper.Map(rooted, rootForm: "ܫܠܡ"),
                LexiconEntryDocumentMapper.Map(unrooted, rootForm: null),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(query: null, grammaticalCategory: null, rootId: rootId, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.RootId.Should().Be(rootId);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyIndex_ReturnsEmptyPage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var service = NewService(client, descriptor, indexName);
        var result = await service.SearchAsync(query: "anything", grammaticalCategory: null, rootId: null, page: 1, pageSize: 10, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }

    private static async Task<(MeilisearchClient Client, IsolatedDescriptor Descriptor, string IndexName)> SetUpIsolatedIndexAsync(MeilisearchFixture fixture, CancellationToken ct)
    {
        var client = fixture.CreateClient();
        var indexName = $"lexicon-query-{Guid.NewGuid():N}";
        var descriptor = new IsolatedDescriptor(indexName);

        var createTask = await client.CreateIndexAsync(indexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);

        var settingsTask = await client.Index(indexName).UpdateSettingsAsync(
            SearchIndexInitializerHostedService.ToMeilisearchSettings(descriptor.Settings),
            ct);
        await client.WaitForTaskAsync(settingsTask.TaskUid, cancellationToken: ct);

        return (client, descriptor, indexName);
    }

    private static LexiconSearchService NewService(
        MeilisearchClient client, IsolatedDescriptor descriptor, string indexName)
    {
        var query = new MeilisearchSearchIndexQuery<LexiconEntrySearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndexQuery<LexiconEntrySearchDocument>>.Instance);
        return new LexiconSearchService(query, NullLogger<LexiconSearchService>.Instance);
    }

    private static async Task<Shared.Pagination.PagedResult<LexiconSearchHitDto>> WaitForHitsAsync(
        Func<Task<Shared.Results.Result<Shared.Pagination.PagedResult<LexiconSearchHitDto>>>> search,
        int expected,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        Shared.Pagination.PagedResult<LexiconSearchHitDto>? last = null;
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
    /// Wraps the production <see cref="LexiconEntryIndexDescriptor"/> with a
    /// test-scoped index name so each test gets isolated documents without
    /// having to coordinate cleanup against the shared Meilisearch container.
    /// </summary>
    private sealed class IsolatedDescriptor : ISearchIndexDescriptor<LexiconEntrySearchDocument>
    {
        private readonly LexiconEntryIndexDescriptor inner = new();

        public IsolatedDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
