using Meilisearch;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Localization;
using Sabro.Shared.Search;

namespace Sabro.IntegrationTests.Lexicon.Application.Search;

[Collection(IntegrationCollection.Name)]
public class LexiconEntrySearchSyncTests
{
    private const string KtbUnvocalized = "ܟܬܒ";

    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;

    public LexiconEntrySearchSyncTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
    }

    [Fact]
    public async Task CreateAsync_PersistsDocumentToMeilisearch()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new LexiconEntryIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);

        var searchIndex = NewSearchIndex(client, descriptor);
        await using var ctx = postgres.CreateLexiconContext();
        var service = new LexiconEntryService(
            ctx,
            new CreateLexiconEntryRequestValidator(),
            new UpdateLexiconEntryRequestValidator(),
            searchIndex,
            Substitute.For<IPronunciationAudioStorage>(),
            Options.Create(new SupportedLanguagesOptions()),
            NullLogger<LexiconEntryService>.Instance);

        var result = await service.CreateAsync(
            new CreateLexiconEntryRequest(
                SyriacUnvocalized: KtbUnvocalized,
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb,
                Meanings: new[] { new CreateLexiconMeaningRequest("en", "to write") }),
            ct);

        result.IsSuccess.Should().BeTrue();
        var doc = await WaitForDocumentAsync(client, descriptor.IndexName, result.Value!.Id.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        doc.SblTransliteration.Should().Be("ktb");
        doc.GrammaticalCategory.Should().Be("Verb");
        doc.MeaningTexts.Should().ContainSingle().Which.Should().Be("to write");
    }

    [Fact]
    public async Task CreateAsync_DenormalizesRootFormIntoDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new LexiconEntryIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);

        var rootForm = "ܫܠܡ";
        Guid rootId;
        await using (var seedCtx = postgres.CreateLexiconContext())
        {
            var rootResult = LexiconRoot.Create(rootForm);
            rootResult.IsSuccess.Should().BeTrue();
            seedCtx.Roots.Add(rootResult.Value!);
            await seedCtx.SaveChangesAsync(ct);
            rootId = rootResult.Value!.Id;
        }

        var searchIndex = NewSearchIndex(client, descriptor);
        await using var ctx = postgres.CreateLexiconContext();
        var service = new LexiconEntryService(
            ctx,
            new CreateLexiconEntryRequestValidator(),
            new UpdateLexiconEntryRequestValidator(),
            searchIndex,
            Substitute.For<IPronunciationAudioStorage>(),
            Options.Create(new SupportedLanguagesOptions()),
            NullLogger<LexiconEntryService>.Instance);

        var result = await service.CreateAsync(
            new CreateLexiconEntryRequest(
                SyriacUnvocalized: "ܫܠܡܐ",
                SblTransliteration: "šlāmā",
                GrammaticalCategory: GrammaticalCategory.Noun,
                RootId: rootId),
            ct);

        result.IsSuccess.Should().BeTrue();
        var doc = await WaitForDocumentAsync(client, descriptor.IndexName, result.Value!.Id.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.RootId.Should().Be(rootId.ToString("D"));
        doc.RootForm.Should().Be(rootForm);
    }

    [Fact]
    public async Task PublishAndSetPlayable_UpdatesLifecycleFieldsInDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new LexiconEntryIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);

        var searchIndex = NewSearchIndex(client, descriptor);
        await using var ctx = postgres.CreateLexiconContext();
        var service = new LexiconEntryService(
            ctx,
            new CreateLexiconEntryRequestValidator(),
            new UpdateLexiconEntryRequestValidator(),
            searchIndex,
            Substitute.For<IPronunciationAudioStorage>(),
            Options.Create(new SupportedLanguagesOptions()),
            NullLogger<LexiconEntryService>.Instance);

        var created = await service.CreateAsync(
            new CreateLexiconEntryRequest(
                SyriacUnvocalized: KtbUnvocalized,
                SblTransliteration: "ktb",
                GrammaticalCategory: GrammaticalCategory.Verb,
                Meanings: new[]
                {
                    new CreateLexiconMeaningRequest("en", "to write"),
                    new CreateLexiconMeaningRequest("fr", "écrire"),
                    new CreateLexiconMeaningRequest("nl", "schrijven"),
                    new CreateLexiconMeaningRequest("de", "schreiben"),
                    new CreateLexiconMeaningRequest("sv", "skriva"),
                }),
            ct);
        created.IsSuccess.Should().BeTrue();
        var id = created.Value!.Id;

        (await service.PublishAsync(id, ct)).IsSuccess.Should().BeTrue();
        (await service.SetPlayableAsync(id, true, ct)).IsSuccess.Should().BeTrue();

        var doc = await WaitForPlayablePublishedAsync(client, descriptor.IndexName, id.ToString("D"), ct);
        doc.Should().NotBeNull();
        doc!.Status.Should().Be(nameof(LexiconEntryStatus.Published));
        doc.PlayableInMeltho.Should().BeTrue();
        doc.PlayableLength.Should().Be(3);
    }

    [Fact]
    public async Task UpsertAsync_WithWaitForTasks_HasReadAfterWriteConsistency()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = meili.CreateClient();
        var descriptor = new LexiconEntryIndexDescriptor();
        await EnsureIndexAsync(client, descriptor, ct);

        var waitingIndex = new MeilisearchSearchIndex<LexiconEntrySearchDocument>(
            client,
            descriptor,
            new MeilisearchOptions { WaitForTasks = true },
            NullLogger<MeilisearchSearchIndex<LexiconEntrySearchDocument>>.Instance);
        var entry = LexiconEntry.Create(
            syriacUnvocalized: "ܨܒܐ",
            sblTransliteration: $"sbl-{Guid.NewGuid():N}",
            grammaticalCategory: GrammaticalCategory.Verb).Value!;
        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        await waitingIndex.UpsertAsync(doc, ct);

        // No polling: WaitForTasks=true guarantees the index has applied the write before UpsertAsync returns.
        var found = await client.Index(descriptor.IndexName)
            .GetDocumentAsync<LexiconEntrySearchDocument>(entry.Id.ToString("D"), cancellationToken: ct);
        found.Should().NotBeNull();
        found.Id.Should().Be(entry.Id.ToString("D"));
    }

    [Fact]
    public async Task UpsertAsync_WhenMeilisearchUnreachable_DoesNotThrow()
    {
        var ct = TestContext.Current.CancellationToken;
        var brokenClient = new MeilisearchClient("http://127.0.0.1:1", apiKey: null);
        var descriptor = new LexiconEntryIndexDescriptor();
        var searchIndex = NewSearchIndex(brokenClient, descriptor);
        var entry = LexiconEntry.Create(KtbUnvocalized, "ktb", GrammaticalCategory.Verb).Value!;
        var doc = LexiconEntryDocumentMapper.Map(entry, rootForm: null);

        var act = async () => await searchIndex.UpsertAsync(doc, ct);

        await act.Should().NotThrowAsync();
    }

    private static MeilisearchSearchIndex<LexiconEntrySearchDocument> NewSearchIndex(
        MeilisearchClient client, LexiconEntryIndexDescriptor descriptor) =>
        new(client, descriptor, new MeilisearchOptions(), NullLogger<MeilisearchSearchIndex<LexiconEntrySearchDocument>>.Instance);

    private static async Task EnsureIndexAsync(
        MeilisearchClient client, LexiconEntryIndexDescriptor descriptor, CancellationToken ct)
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

    private static async Task<LexiconEntrySearchDocument?> WaitForDocumentAsync(
        MeilisearchClient client, string indexName, string documentId, CancellationToken ct)
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

    private static async Task<LexiconEntrySearchDocument?> WaitForPlayablePublishedAsync(
        MeilisearchClient client, string indexName, string documentId, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        LexiconEntrySearchDocument? doc = null;
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                doc = await client.Index(indexName).GetDocumentAsync<LexiconEntrySearchDocument>(documentId, cancellationToken: ct);
                if (doc is not null && doc.Status == nameof(LexiconEntryStatus.Published) && doc.PlayableInMeltho)
                {
                    return doc;
                }
            }
            catch (MeilisearchApiError)
            {
            }

            await Task.Delay(150, ct);
        }

        return doc;
    }
}
