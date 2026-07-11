using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sabro.API.Controllers.V1;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Application.Dictionary;
using Sabro.Lexicon.Domain;
using Sabro.Play.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The anonymous dictionary surface. Two contract guarantees matter beyond the
/// usual published-only rule: the payloads never carry <c>status</c> or
/// <c>playableInMeltho</c> (the future puzzle pool must not be enumerable),
/// and <c>playedInMeltho</c> only turns true the day after a word was served
/// (today's live puzzle stays unmarked).
/// </summary>
[Collection(IntegrationCollection.Name)]
public class DictionaryControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public DictionaryControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task List_ReturnsOnlyPublishedEntries_WithoutPoolMarkers()
    {
        var ct = TestContext.Current.CancellationToken;
        var publishedId = await SeedEntryAsync("ܡܠܟܐ", publish: true, playable: true, ct);
        var draftId = await SeedEntryAsync("ܟܬܒ", publish: false, playable: false, ct);

        var response = await client.GetAsync("/api/v1/dictionary?pageSize=200", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync(ct);
        raw.Should().NotContain("playableInMeltho").And.NotContain("\"status\"");

        var page = JsonSerializer.Deserialize<PagedResult<DictionaryEntryListItem>>(raw, SabroApiFactory.JsonOptions);
        page!.Items.Select(i => i.Id).Should().Contain(publishedId).And.NotContain(draftId);
    }

    [Fact]
    public async Task GetById_OnDraft_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var draftId = await SeedEntryAsync("ܟܬܒ", publish: false, playable: false, ct);

        var response = await client.GetAsync($"/api/v1/dictionary/{draftId}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_OnNeverServedWord_ReportsNotPlayed()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await SeedEntryAsync("ܡܠܟܐ", publish: true, playable: true, ct);

        var dto = await GetDetailAsync(id, ct);

        dto.PlayedInMeltho.Should().BeFalse();
        dto.LetterCount.Should().Be(4);
        dto.Meanings.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_OnWordServedYesterday_ReportsPlayed()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await SeedEntryAsync("ܫܠܡܐ", publish: true, playable: true, ct);
        await SeedPuzzleAsync(id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), ct);

        var dto = await GetDetailAsync(id, ct);

        dto.PlayedInMeltho.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_OnTodaysLivePuzzle_StaysUnmarked()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await SeedEntryAsync("ܪܒܐ", publish: true, playable: true, ct);
        await SeedPuzzleAsync(id, DateOnly.FromDateTime(DateTime.UtcNow), ct);

        var dto = await GetDetailAsync(id, ct);

        dto.PlayedInMeltho.Should().BeFalse();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<DictionaryEntryDetailResponse> GetDetailAsync(Guid id, CancellationToken ct)
    {
        var response = await client.GetAsync($"/api/v1/dictionary/{id}", ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<DictionaryEntryDetailResponse>(SabroApiFactory.JsonOptions, ct);
        return dto!;
    }

    private async Task<Guid> SeedEntryAsync(string unvocalized, bool publish, bool playable, CancellationToken ct)
    {
        var meanings = new[]
        {
            LexiconMeaning.Create("en", "gloss").Value!,
            LexiconMeaning.Create("fr", "glose").Value!,
            LexiconMeaning.Create("nl", "glos").Value!,
        };
        var entry = LexiconEntry.Create(unvocalized, "translit", GrammaticalCategory.Noun, meanings: meanings).Value!;
        if (publish)
        {
            entry.Publish().Should().BeNull();
        }

        if (playable)
        {
            entry.SetPlayable(true).Should().BeNull();
        }

        await using var ctx = postgres.CreateLexiconContext();
        ctx.Entries.Add(entry);
        await ctx.SaveChangesAsync(ct);
        return entry.Id;
    }

    private async Task SeedPuzzleAsync(Guid lexiconEntryId, DateOnly date, CancellationToken ct)
    {
        await using var ctx = postgres.CreatePlayContext();
        ctx.MelthoDailyPuzzles.Add(MelthoDailyPuzzle.Create(Games.Meltho, date, lexiconEntryId).Value!);
        await ctx.SaveChangesAsync(ct);
    }
}
