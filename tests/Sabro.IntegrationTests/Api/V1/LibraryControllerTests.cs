using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sabro.API.Controllers.V1;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Domain;
using Sabro.Play.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The unified `/api/v1/library` endpoint behind Sabro's own `/library` page. Additive: these
/// tests only cover the new route, plus one regression check that the existing
/// `/api/v1/dictionary` and `/play/meltho/library` contracts are untouched.
///
/// Puzzle-seeding tests use the real "today"/"yesterday" (the controller resolves "today" from
/// the system clock, not an injectable TimeProvider, unlike the Application-layer tests) — so,
/// same as MelthoLibraryServiceTests, each one clears meltho_daily_puzzles first: the unique
/// index is (GameId, Date), and this collection runs sequentially against one shared database, so
/// any two tests (in this file or DictionaryControllerTests) seeding the same real calendar date
/// would otherwise collide.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class LibraryControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public LibraryControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task List_Default_ReturnsPublishedWordsOnly_WithNullStatsWhenNeverPlayed()
    {
        var ct = TestContext.Current.CancellationToken;
        var published = await SeedEntryAsync("ܡܠܟܐ", publish: true, ct);
        var draft = await SeedEntryAsync("ܟܬܒ", publish: false, ct);

        var page = await GetPageAsync("/api/v1/library", ct);

        var ids = page.Items.Select(i => i.Id).ToList();
        ids.Should().Contain(published).And.NotContain(draft);
        page.Items.Single(i => i.Id == published).LastPlayedOn.Should().BeNull();
        page.Items.Single(i => i.Id == published).TimesPlayed.Should().BeNull();
    }

    [Fact]
    public async Task List_Default_EnrichesWithStats_WhenWordWasPlayed()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var id = await SeedEntryAsync("ܫܠܡܐ", publish: true, ct);
        await SeedPuzzleAsync(id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), ct);

        var page = await GetPageAsync("/api/v1/library", ct);

        var item = page.Items.Single(i => i.Id == id);
        item.LastPlayedOn.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));
        item.TimesPlayed.Should().Be(1);
    }

    [Fact]
    public async Task List_Default_WithRecentSort_ReturnsValidationError()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/api/v1/library?sort=Recent", ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_Default_LengthSort_OrdersByLetterCount()
    {
        var ct = TestContext.Current.CancellationToken;
        var longer = await SeedEntryAsync("ܡܰܠܰܐܟ݂ܳܐ", publish: true, ct);
        var shorter = await SeedEntryAsync("ܝܰܡܳܐ", publish: true, ct);

        var page = await GetPageAsync("/api/v1/library?sort=Length", ct);

        var ids = page.Items.Select(i => i.Id).ToList();
        ids.IndexOf(shorter).Should().BeLessThan(ids.IndexOf(longer));
    }

    [Fact]
    public async Task List_Default_Search_MatchesGloss()
    {
        var ct = TestContext.Current.CancellationToken;
        var match = await SeedEntryAsync("ܡܠܟܐ", publish: true, ct, gloss: "king");
        var nonMatch = await SeedEntryAsync("ܪܒܐ", publish: true, ct, gloss: "great");

        var page = await GetPageAsync("/api/v1/library?search=king", ct);

        var ids = page.Items.Select(i => i.Id).ToList();
        ids.Should().Contain(match).And.NotContain(nonMatch);
    }

    [Fact]
    public async Task List_PlayedInMeltho_ExcludesNeverPlayedWords()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var played = await SeedEntryAsync("ܫܠܡܐ", publish: true, ct);
        var neverPlayed = await SeedEntryAsync("ܪܒܐ", publish: true, ct);
        await SeedPuzzleAsync(played, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), ct);

        var page = await GetPageAsync("/api/v1/library?playedInMeltho=true", ct);

        var ids = page.Items.Select(i => i.Id).ToList();
        ids.Should().Contain(played).And.NotContain(neverPlayed);
    }

    [Fact]
    public async Task List_PlayedInMeltho_ExcludesWordsUnpublishedSincePlayed()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var id = await SeedEntryAsync("ܡܠܬܐ", publish: true, ct);
        await SeedPuzzleAsync(id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), ct);
        await ReturnToDraftAsync(id, ct);

        var page = await GetPageAsync("/api/v1/library?playedInMeltho=true", ct);

        page.Items.Select(i => i.Id).Should().NotContain(id);
    }

    [Fact]
    public async Task List_PlayedInMeltho_ExcludesTodaysLiveWord()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var id = await SeedEntryAsync("ܪܒܐ", publish: true, ct);
        await SeedPuzzleAsync(id, DateOnly.FromDateTime(DateTime.UtcNow), ct);

        var page = await GetPageAsync("/api/v1/library?playedInMeltho=true", ct);

        page.Items.Select(i => i.Id).Should().NotContain(id);
    }

    [Fact]
    public async Task List_PlayedInMeltho_RecentSort_IsAccepted()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var id = await SeedEntryAsync("ܛܘܪܐ", publish: true, ct);
        await SeedPuzzleAsync(id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), ct);

        var response = await client.GetAsync("/api/v1/library?playedInMeltho=true&sort=Recent", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_Regression_DictionaryAndMelthoLibraryEndpointsStillWork()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);
        var id = await SeedEntryAsync("ܟܘܟܒܐ", publish: true, ct);
        await SeedPuzzleAsync(id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), ct);

        var dictionaryResponse = await client.GetAsync("/api/v1/dictionary?pageSize=200", ct);
        var melthoResponse = await client.GetAsync("/api/v1/play/meltho/library", ct);

        dictionaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        melthoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<PagedResult<UnifiedLibraryEntryDto>> GetPageAsync(string requestUri, CancellationToken ct)
    {
        var response = await client.GetAsync(requestUri, ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<PagedResult<UnifiedLibraryEntryDto>>(raw, SabroApiFactory.JsonOptions)!;
    }

    private async Task<Guid> SeedEntryAsync(string unvocalized, bool publish, CancellationToken ct, string gloss = "gloss")
    {
        var meanings = new[]
        {
            LexiconMeaning.Create("en", gloss).Value!,
            LexiconMeaning.Create("fr", "glose").Value!,
            LexiconMeaning.Create("nl", "glos").Value!,
            LexiconMeaning.Create("de", "Glosse").Value!,
            LexiconMeaning.Create("sv", "glosa").Value!,
        };
        var entry = LexiconEntry.Create(unvocalized, "translit", GrammaticalCategory.Noun, meanings: meanings).Value!;
        if (publish)
        {
            entry.Publish().Should().BeNull();
        }

        await using var ctx = postgres.CreateLexiconContext();
        ctx.Entries.Add(entry);
        await ctx.SaveChangesAsync(ct);
        return entry.Id;
    }

    private async Task ReturnToDraftAsync(Guid id, CancellationToken ct)
    {
        await using var ctx = postgres.CreateLexiconContext();
        var entry = await ctx.Entries.FindAsync(new object?[] { id }, ct);
        entry!.ReturnToDraft();
        await ctx.SaveChangesAsync(ct);
    }

    private async Task SeedPuzzleAsync(Guid lexiconEntryId, DateOnly date, CancellationToken ct)
    {
        await using var ctx = postgres.CreatePlayContext();
        ctx.MelthoDailyPuzzles.Add(MelthoDailyPuzzle.Create(Games.Meltho, date, lexiconEntryId).Value!);
        await ctx.SaveChangesAsync(ct);
    }

    private async Task ClearAsync(CancellationToken ct)
    {
        await using var ctx = postgres.CreatePlayContext();
        await ctx.MelthoDailyPuzzles.ExecuteDeleteAsync(ct);
    }
}
