using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Pagination;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.SearchInfrastructure;

/// <summary>
/// Verifies the two shared search-infra gaps fixed for the admin Lexicon
/// backoffice search plan against a real Meilisearch instance rather than by
/// assumption: a <see cref="SearchFilter"/> with <c>Raw: true</c> must match a
/// boolean field unquoted (Meilisearch rejects <c>field = "true"</c> as a
/// filter on a boolean attribute), and a <see cref="SearchSort"/> must actually
/// order results.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class MeilisearchSearchIndexQuerySortAndRawFilterTests
{
    private readonly MeilisearchFixture meili;

    public MeilisearchSearchIndexQuerySortAndRawFilterTests(MeilisearchFixture meili)
    {
        this.meili = meili;
    }

    [Fact]
    public async Task SearchAsync_WithRawTrueFilter_MatchesUnquotedBooleanField()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName, query) = await SetUpAsync(ct);

        var docs = new[]
        {
            new Doc { Id = "1", Name = "alpha", Flag = true, Rank = 2 },
            new Doc { Id = "2", Name = "beta", Flag = false, Rank = 1 },
        };
        await client.Index(indexName).AddDocumentsAsync(docs, descriptor.PrimaryKey, ct);

        var request = new SearchRequest(
            Query: null,
            Page: 1,
            PageSize: 10,
            Filters: new[] { new SearchFilter("flag", "true", Raw: true) });

        var result = await WaitForHitsAsync(query, request, expected: 1, ct);

        result.Items.Should().ContainSingle().Which.Id.Should().Be("1");
    }

    [Fact]
    public async Task SearchAsync_WithRawFalseFilter_MatchesUnquotedBooleanField()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName, query) = await SetUpAsync(ct);

        var docs = new[]
        {
            new Doc { Id = "1", Name = "alpha", Flag = true, Rank = 2 },
            new Doc { Id = "2", Name = "beta", Flag = false, Rank = 1 },
        };
        await client.Index(indexName).AddDocumentsAsync(docs, descriptor.PrimaryKey, ct);

        var request = new SearchRequest(
            Query: null,
            Page: 1,
            PageSize: 10,
            Filters: new[] { new SearchFilter("flag", "false", Raw: true) });

        var result = await WaitForHitsAsync(query, request, expected: 1, ct);

        result.Items.Should().ContainSingle().Which.Id.Should().Be("2");
    }

    [Fact]
    public async Task SearchAsync_WithSortAscending_OrdersByRequestedField()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName, query) = await SetUpAsync(ct);

        var docs = new[]
        {
            new Doc { Id = "1", Name = "alpha", Flag = true, Rank = 2 },
            new Doc { Id = "2", Name = "beta", Flag = true, Rank = 1 },
            new Doc { Id = "3", Name = "gamma", Flag = true, Rank = 3 },
        };
        await client.Index(indexName).AddDocumentsAsync(docs, descriptor.PrimaryKey, ct);

        var request = new SearchRequest(
            Query: null,
            Page: 1,
            PageSize: 10,
            Sort: new[] { new SearchSort("rank") });

        var result = await WaitForHitsAsync(query, request, expected: 3, ct);

        result.Items.Select(d => d.Id).Should().ContainInConsecutiveOrder("2", "1", "3");
    }

    [Fact]
    public async Task SearchAsync_WithSortDescending_ReversesOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName, query) = await SetUpAsync(ct);

        var docs = new[]
        {
            new Doc { Id = "1", Name = "alpha", Flag = true, Rank = 2 },
            new Doc { Id = "2", Name = "beta", Flag = true, Rank = 1 },
            new Doc { Id = "3", Name = "gamma", Flag = true, Rank = 3 },
        };
        await client.Index(indexName).AddDocumentsAsync(docs, descriptor.PrimaryKey, ct);

        var request = new SearchRequest(
            Query: null,
            Page: 1,
            PageSize: 10,
            Sort: new[] { new SearchSort("rank", Descending: true) });

        var result = await WaitForHitsAsync(query, request, expected: 3, ct);

        result.Items.Select(d => d.Id).Should().ContainInConsecutiveOrder("3", "1", "2");
    }

    private static async Task<PagedResult<Doc>> WaitForHitsAsync(
        MeilisearchSearchIndexQuery<Doc> query,
        SearchRequest request,
        int expected,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        PagedResult<Doc>? last = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            last = await query.SearchAsync(request, ct);
            if (last.Total >= expected)
            {
                return last;
            }

            await Task.Delay(150, ct);
        }

        throw new Xunit.Sdk.XunitException($"Expected at least {expected} hit(s), got {last?.Total ?? 0} within timeout.");
    }

    private async Task<(MeilisearchClient Client, IsolatedDescriptor Descriptor, string IndexName, MeilisearchSearchIndexQuery<Doc> Query)> SetUpAsync(CancellationToken ct)
    {
        var client = meili.CreateClient();
        var indexName = $"sort-raw-filter-{Guid.NewGuid():N}";
        var descriptor = new IsolatedDescriptor(indexName);

        var createTask = await client.CreateIndexAsync(indexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);

        var settingsTask = await client.Index(indexName).UpdateSettingsAsync(
            SearchIndexInitializerHostedService.ToMeilisearchSettings(descriptor.Settings),
            ct);
        await client.WaitForTaskAsync(settingsTask.TaskUid, cancellationToken: ct);

        var query = new MeilisearchSearchIndexQuery<Doc>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndexQuery<Doc>>.Instance);

        return (client, descriptor, indexName, query);
    }

    private sealed class Doc
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool Flag { get; set; }

        public int Rank { get; set; }
    }

    private sealed class IsolatedDescriptor : ISearchIndexDescriptor<Doc>
    {
        private static readonly string[] Searchable = { "name" };

        private static readonly string[] Filterable = { "flag" };

        private static readonly string[] Sortable = { "rank" };

        public IsolatedDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => "id";

        public IndexSettings Settings => new(
            SearchableAttributes: Searchable,
            FilterableAttributes: Filterable,
            Synonyms: new Dictionary<string, IReadOnlyList<string>>(),
            SortableAttributes: Sortable);
    }
}
