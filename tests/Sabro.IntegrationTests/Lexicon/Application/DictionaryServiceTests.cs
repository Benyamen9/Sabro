using Sabro.Lexicon.Application.Dictionary;
using Sabro.Lexicon.Domain;

namespace Sabro.IntegrationTests.Lexicon.Application;

/// <summary>
/// The public dictionary read surface: every published entry, alphabetical,
/// with a detail projection that reuses the library composition. Drafts are
/// editorial state and must never surface here.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class DictionaryServiceTests
{
    private readonly PostgresFixture fixture;

    public DictionaryServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyPublishedEntries()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var published = await SeedAsync(ctx, "ܡܠܟܐ", "malko", publish: true, ct);
        var draft = await SeedAsync(ctx, "ܟܬܒ", "ktb", publish: false, ct);
        var service = new DictionaryService(ctx);

        var result = await service.ListAsync(page: 1, pageSize: 200, category: null, ct);

        result.IsSuccess.Should().BeTrue();
        var ids = result.Value!.Items.Select(i => i.Id).ToList();
        ids.Should().Contain(published).And.NotContain(draft);
    }

    [Fact]
    public async Task ListAsync_OrdersAlphabeticallyBySyriacForm()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();

        // Seed out of alphabetical order: taw (ܬ) sorts after beth (ܒ).
        var taw = await SeedAsync(ctx, "ܬܪܥܐ", "tarʿo", publish: true, ct);
        var beth = await SeedAsync(ctx, "ܒܝܬܐ", "bayto", publish: true, ct);
        var service = new DictionaryService(ctx);

        var result = await service.ListAsync(page: 1, pageSize: 200, category: null, ct);

        result.IsSuccess.Should().BeTrue();
        var ids = result.Value!.Items.Select(i => i.Id).ToList();
        ids.IndexOf(beth).Should().BeLessThan(ids.IndexOf(taw));
    }

    [Fact]
    public async Task ListAsync_FiltersByGrammaticalCategory()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var noun = await SeedAsync(ctx, "ܡܠܟܐ", "malko", publish: true, ct, GrammaticalCategory.Noun);
        var adjective = await SeedAsync(ctx, "ܪܒܐ", "rabo", publish: true, ct, GrammaticalCategory.Adjective);
        var service = new DictionaryService(ctx);

        var result = await service.ListAsync(page: 1, pageSize: 200, GrammaticalCategory.Adjective, ct);

        result.IsSuccess.Should().BeTrue();
        var ids = result.Value!.Items.Select(i => i.Id).ToList();
        ids.Should().Contain(adjective).And.NotContain(noun);
        result.Value.Items.Should().OnlyContain(i => i.GrammaticalCategory == nameof(GrammaticalCategory.Adjective));
    }

    [Fact]
    public async Task ListAsync_WithInvalidPaging_ReturnsValidationFailure()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = new DictionaryService(ctx);

        var result = await service.ListAsync(page: -1, pageSize: 500, category: null, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task GetByIdAsync_OnPublishedEntry_ReturnsDetailWithComposition()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var root = LexiconRoot.Create("ܡܠܟ").Value!;
        ctx.Roots.Add(root);
        var id = await SeedAsync(ctx, "ܡܠܟܐ", "malko", publish: true, ct, vocalized: "ܡܰܠܟ݁ܳܐ", rootId: root.Id);
        var service = new DictionaryService(ctx);

        var result = await service.GetByIdAsync(id, ct);

        result.IsSuccess.Should().BeTrue();
        var detail = result.Value!;
        detail.SyriacUnvocalized.Should().Be("ܡܠܟܐ");
        detail.SyriacVocalized.Should().Be("ܡܰܠܟ݁ܳܐ");
        detail.Root.Should().Be("ܡܠܟ");
        detail.Composition.Should().NotBeEmpty();
        detail.Meanings.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetByIdAsync_OnDraftEntry_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var id = await SeedAsync(ctx, "ܟܬܒ", "ktb", publish: false, ct);
        var service = new DictionaryService(ctx);

        var result = await service.GetByIdAsync(id, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetByIdAsync_OnMissingEntry_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var service = new DictionaryService(ctx);

        var result = await service.GetByIdAsync(Guid.NewGuid(), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    private static async Task<Guid> SeedAsync(
        global::Sabro.Lexicon.Infrastructure.LexiconDbContext ctx,
        string unvocalized,
        string transliteration,
        bool publish,
        CancellationToken ct,
        GrammaticalCategory category = GrammaticalCategory.Noun,
        string? vocalized = null,
        Guid? rootId = null)
    {
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "gloss").Value!,
            LexiconMeaning.Create("fr", "glose").Value!,
            LexiconMeaning.Create("nl", "glos").Value!,
            LexiconMeaning.Create("de", "Glosse").Value!,
            LexiconMeaning.Create("sv", "glosa").Value!,
        };
        var entry = LexiconEntry.Create(
            unvocalized,
            transliteration,
            category,
            syriacVocalized: vocalized,
            rootId: rootId,
            meanings: meanings).Value!;
        if (publish)
        {
            entry.Publish().Should().BeNull();
        }

        ctx.Entries.Add(entry);
        await ctx.SaveChangesAsync(ct);
        return entry.Id;
    }
}
