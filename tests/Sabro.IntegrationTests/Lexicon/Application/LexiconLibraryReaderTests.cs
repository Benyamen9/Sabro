using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;

namespace Sabro.IntegrationTests.Lexicon.Application;

/// <summary>
/// The cross-module read surface Play consumes for the Meltho library. Unlike
/// <c>DictionaryService</c>, the list/detail lookups here deliberately do not re-check
/// eligibility (a served word must keep rendering even once unpublished) — only
/// <see cref="ILexiconLibraryReader.GetPublishedIdsAsync"/> checks editorial state, because
/// callers use it specifically to ask "is this one still in the dictionary today".
/// </summary>
[Collection(IntegrationCollection.Name)]
public class LexiconLibraryReaderTests
{
    private readonly PostgresFixture fixture;

    public LexiconLibraryReaderTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetLibraryListAsync_IncludesGrammaticalCategory()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var id = await SeedAsync(ctx, "ܪܒܐ", "rabo", publish: true, ct, GrammaticalCategory.Adjective);
        var reader = new LexiconLibraryReader(ctx);

        var result = await reader.GetLibraryListAsync(new[] { id }, ct);

        result.Should().ContainSingle().Which.GrammaticalCategory.Should().Be(nameof(GrammaticalCategory.Adjective));
    }

    [Fact]
    public async Task GetLibraryListAsync_DoesNotCheckPublishState()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var draftId = await SeedAsync(ctx, "ܟܬܒ", "ktb", publish: false, ct);
        var reader = new LexiconLibraryReader(ctx);

        var result = await reader.GetLibraryListAsync(new[] { draftId }, ct);

        result.Should().ContainSingle().Which.Id.Should().Be(draftId);
    }

    [Fact]
    public async Task GetPublishedIdsAsync_ReturnsOnlyCurrentlyPublishedIds()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var published = await SeedAsync(ctx, "ܡܠܟܐ", "malko", publish: true, ct);
        var draft = await SeedAsync(ctx, "ܟܬܒ", "ktb", publish: false, ct);
        var reader = new LexiconLibraryReader(ctx);

        var result = await reader.GetPublishedIdsAsync(new[] { published, draft }, ct);

        result.Should().ContainSingle().Which.Should().Be(published);
    }

    [Fact]
    public async Task GetPublishedIdsAsync_UnknownId_IsSimplyAbsent()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var reader = new LexiconLibraryReader(ctx);

        var result = await reader.GetPublishedIdsAsync(new[] { Guid.NewGuid() }, ct);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublishedIdsAsync_EmptyInput_ReturnsEmptyWithoutQuerying()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreateLexiconContext();
        var reader = new LexiconLibraryReader(ctx);

        var result = await reader.GetPublishedIdsAsync(Array.Empty<Guid>(), ct);

        result.Should().BeEmpty();
    }

    private static async Task<Guid> SeedAsync(
        global::Sabro.Lexicon.Infrastructure.LexiconDbContext ctx,
        string unvocalized,
        string transliteration,
        bool publish,
        CancellationToken ct,
        GrammaticalCategory category = GrammaticalCategory.Noun)
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
