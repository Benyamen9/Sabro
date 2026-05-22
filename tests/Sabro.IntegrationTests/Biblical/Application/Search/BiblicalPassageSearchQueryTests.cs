using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Biblical.Application.Search;

[Collection(IntegrationCollection.Name)]
public class BiblicalPassageSearchQueryTests
{
    private readonly MeilisearchFixture meili;

    public BiblicalPassageSearchQueryTests(MeilisearchFixture meili)
    {
        this.meili = meili;
    }

    [Fact]
    public async Task SearchAsync_OverEnglishName_ReturnsIndexedPassage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var book = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40, syriacName: "ܡܬܝ").Value!;
        var passage = BiblicalPassage.Create(book.Id, 3, 7).Value!;
        var doc = BiblicalPassageDocumentMapper.Map(passage, book);
        await client.Index(indexName).AddDocumentsAsync(new[] { doc }, descriptor.PrimaryKey, ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync("matthew", bookCode: null, testament: null, chapterNumber: null, verseNumber: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.Id.Should().Be(passage.Id);
    }

    [Fact]
    public async Task SearchAsync_OverSyriacName_ReturnsIndexedPassage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var book = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40, syriacName: "ܡܬܝ").Value!;
        var passage = BiblicalPassage.Create(book.Id, 1, 1).Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[] { BiblicalPassageDocumentMapper.Map(passage, book) }, descriptor.PrimaryKey, ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync("ܡܬܝ", bookCode: null, testament: null, chapterNumber: null, verseNumber: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.BookSyriacName.Should().Be("ܡܬܝ");
    }

    [Fact]
    public async Task SearchAsync_WithBookCodeFilter_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var matthew = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40).Value!;
        var john = BiblicalBook.Create("JHN", "John", Testament.New, order: 43).Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                BiblicalPassageDocumentMapper.Map(BiblicalPassage.Create(matthew.Id, 1, 1).Value!, matthew),
                BiblicalPassageDocumentMapper.Map(BiblicalPassage.Create(john.Id, 1, 1).Value!, john),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(query: null, bookCode: "JHN", testament: null, chapterNumber: null, verseNumber: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.BookCode.Should().Be("JHN");
    }

    [Fact]
    public async Task SearchAsync_WithTestamentFilter_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var genesis = BiblicalBook.Create("GEN", "Genesis", Testament.Old, order: 1).Value!;
        var matthew = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40).Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                BiblicalPassageDocumentMapper.Map(BiblicalPassage.Create(genesis.Id, 1, 1).Value!, genesis),
                BiblicalPassageDocumentMapper.Map(BiblicalPassage.Create(matthew.Id, 1, 1).Value!, matthew),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(query: null, bookCode: null, testament: Testament.Old, chapterNumber: null, verseNumber: null, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        hits.Items.Should().ContainSingle().Which.Testament.Should().Be(Testament.Old);
    }

    [Fact]
    public async Task SearchAsync_WithChapterAndVerseFilters_RestrictsResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var book = BiblicalBook.Create("MAT", "Matthew", Testament.New, order: 40).Value!;
        await client.Index(indexName).AddDocumentsAsync(
            new[]
            {
                BiblicalPassageDocumentMapper.Map(BiblicalPassage.Create(book.Id, 1, 1).Value!, book),
                BiblicalPassageDocumentMapper.Map(BiblicalPassage.Create(book.Id, 3, 7).Value!, book),
                BiblicalPassageDocumentMapper.Map(BiblicalPassage.Create(book.Id, 3, 16).Value!, book),
            },
            descriptor.PrimaryKey,
            ct);

        var service = NewService(client, descriptor, indexName);
        var hits = await WaitForHitsAsync(
            () => service.SearchAsync(query: null, bookCode: null, testament: null, chapterNumber: 3, verseNumber: 7, page: 1, pageSize: 10, ct),
            expected: 1,
            ct);

        var hit = hits.Items.Should().ContainSingle().Subject;
        hit.ChapterNumber.Should().Be(3);
        hit.VerseNumber.Should().Be(7);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyIndex_ReturnsEmptyPage()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, descriptor, indexName) = await SetUpIsolatedIndexAsync(meili, ct);

        var service = NewService(client, descriptor, indexName);
        var result = await service.SearchAsync(
            query: "anything",
            bookCode: null,
            testament: null,
            chapterNumber: null,
            verseNumber: null,
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
        var indexName = $"biblical-passages-query-{Guid.NewGuid():N}";
        var descriptor = new IsolatedDescriptor(indexName);

        var createTask = await client.CreateIndexAsync(indexName, descriptor.PrimaryKey, ct);
        await client.WaitForTaskAsync(createTask.TaskUid, cancellationToken: ct);

        var settingsTask = await client.Index(indexName).UpdateSettingsAsync(
            SearchIndexInitializerHostedService.ToMeilisearchSettings(descriptor.Settings),
            ct);
        await client.WaitForTaskAsync(settingsTask.TaskUid, cancellationToken: ct);

        return (client, descriptor, indexName);
    }

    private static BiblicalPassageSearchService NewService(
        MeilisearchClient client, IsolatedDescriptor descriptor, string indexName)
    {
        var query = new MeilisearchSearchIndexQuery<BiblicalPassageSearchDocument>(
            client,
            descriptor,
            NullLogger<MeilisearchSearchIndexQuery<BiblicalPassageSearchDocument>>.Instance);
        return new BiblicalPassageSearchService(query, NullLogger<BiblicalPassageSearchService>.Instance);
    }

    private static async Task<Shared.Pagination.PagedResult<BiblicalPassageSearchHitDto>> WaitForHitsAsync(
        Func<Task<Shared.Results.Result<Shared.Pagination.PagedResult<BiblicalPassageSearchHitDto>>>> search,
        int expected,
        CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        Shared.Pagination.PagedResult<BiblicalPassageSearchHitDto>? last = null;
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
    /// Wraps the production <see cref="BiblicalPassageIndexDescriptor"/> with a
    /// test-scoped index name so each test gets isolated documents without
    /// fighting over the shared Meilisearch container.
    /// </summary>
    private sealed class IsolatedDescriptor : ISearchIndexDescriptor<BiblicalPassageSearchDocument>
    {
        private readonly BiblicalPassageIndexDescriptor inner = new();

        public IsolatedDescriptor(string indexName)
        {
            IndexName = indexName;
        }

        public string IndexName { get; }

        public string PrimaryKey => inner.PrimaryKey;

        public IndexSettings Settings => inner.Settings;
    }
}
