using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Sabro.BethGazo.Application.Chants;
using Sabro.IntegrationTests.Api;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The public Beth Gazo surface at <c>/api/v1/chants</c> — the answer lists Nahlo
/// plays against.
/// </summary>
/// <remarks>
/// Most of these guard a property rather than a behaviour: that the endpoint cannot
/// be used to <i>look up</i> the answer. The chant's text identifies it outright, so
/// anything here that pairs a melody with its mode, or narrows a list to the puzzle
/// pool, ends the game quietly and without failing anything.
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class ChantsControllerTests : IDisposable
{
    private const string Incipit = "ܡܪܝܡ";

    /// <summary>The seeded Farde section — it admits every mode, so any mode is valid here.</summary>
    private static readonly Guid Farde = Guid.Parse("7a2c4b20-0000-4000-8000-000000000001");

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;
    private readonly List<string> uploadedUrls = new();

    public ChantsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        // Publishing needs a recording, and recordings land in the API project's
        // wwwroot — shared with the developer's own files, so leave nothing behind.
        if (uploadedUrls.Count > 0)
        {
            var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();
            foreach (var url in uploadedUrls)
            {
                var path = Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot",
                    url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AnswerOptions_AreServedAnonymously()
    {
        // Nahlo is played without an account, like Meltho and Shmo.
        var ct = TestContext.Current.CancellationToken;
        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Authorization = null;

        var response = await anonymous.GetAsync("/api/v1/chants/answer-options", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnswerOptions_ListEveryMode_EvenOnesNoChantUses()
    {
        // Trimming the modes to those actually in use would narrow the answer space
        // for free — a player could rule out five of eight without listening.
        var ct = TestContext.Current.CancellationToken;

        var options = await AnswerOptionsAsync(ct);

        options.Modes.Count.Should().BeGreaterThanOrEqualTo(8);
        options.Modes.Select(m => m.Position).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task AnswerOptions_IncludeAPublishedMelody()
    {
        var ct = TestContext.Current.CancellationToken;
        await PublishAsync("Answer option melody", shuhlofoNumber: 1, ct);

        var options = await AnswerOptionsAsync(ct);

        options.Melodies.Should().Contain("Answer option melody");
    }

    [Fact]
    public async Task AnswerOptions_ExcludeDrafts()
    {
        // A draft is unfinished editorial data. Publishing is what makes a melody
        // part of the public answer space.
        var ct = TestContext.Current.CancellationToken;
        await CreateDraftAsync("Draft only melody", shuhlofoNumber: 1, ct);

        var options = await AnswerOptionsAsync(ct);

        options.Melodies.Should().NotContain("Draft only melody");
    }

    [Fact]
    public async Task AnswerOptions_IncludeAPublishedMelodyThatIsNotInThePool()
    {
        // The load-bearing one. If the melody list were drawn from the playable pool
        // it would tell the player the answer is one of these few — the endpoint
        // would become a way of narrowing the round instead of answering it.
        var ct = TestContext.Current.CancellationToken;
        await PublishAsync("Published unplayable melody", shuhlofoNumber: null, ct);

        var options = await AnswerOptionsAsync(ct);

        options.Melodies.Should().Contain(
            "Published unplayable melody",
            "the answer space must be every published melody, never just the pool");
    }

    [Fact]
    public async Task AnswerOptions_NeverPairAMelodyWithItsMode()
    {
        // The property the whole endpoint exists to preserve: three lists, no rows.
        // A player who recognises the text must still have to know the mode.
        var ct = TestContext.Current.CancellationToken;
        await PublishAsync("Unpaired melody", shuhlofoNumber: null, ct);

        var response = await client.GetAsync("/api/v1/chants/answer-options", ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        // The payload carries exactly three arrays. Anything that let a melody sit
        // beside a mode — a chant list, an object per melody — would show up here as
        // a key this endpoint has no business having.
        json.Should().NotContain("modeName", "a melody must never travel next to its mode");
        json.Should().NotContain("chantId");
        json.Should().NotContain("audioUrl", "recordings are the puzzle, not a lookup");
        json.Should().NotContain("playableInNahlo", "the pool must not be enumerable from here");
    }

    [Fact]
    public async Task AnswerOptions_ListEachMelodyOnce()
    {
        // A melody name recurs across modes, so the same name is several chants. The
        // count of entries would otherwise hint at how many modes it appears in.
        var ct = TestContext.Current.CancellationToken;
        await PublishAsync("Repeated melody", shuhlofoNumber: null, ct, modePosition: 1);
        await PublishAsync("Repeated melody", shuhlofoNumber: null, ct, modePosition: 2);

        var options = await AnswerOptionsAsync(ct);

        options.Melodies.Count(m => m == "Repeated melody").Should().Be(1);
        options.Melodies.Should().BeInAscendingOrder();
    }

    private async Task<ChantAnswerOptionsDto> AnswerOptionsAsync(CancellationToken ct)
    {
        var response = await client.GetAsync("/api/v1/chants/answer-options", ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ChantAnswerOptionsDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    private async Task<Guid> CreateDraftAsync(
        string transliteration,
        int? shuhlofoNumber,
        CancellationToken ct,
        int modePosition = 1)
    {
        await using var ctx = postgres.CreateBethGazoContext();
        var mode = ctx.Modes.OrderBy(m => m.Position).Skip(modePosition - 1).First();

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/chants",
            new CreateChantRequest(Incipit, transliteration, Farde, mode.Id, ShuhlofoNumber: shuhlofoNumber),
            ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct);
        return dto!.Id;
    }

    /// <summary>
    /// Creates a chant and publishes it — which needs a recording, since a chant
    /// without one is not a puzzle. Left out of the playable pool deliberately: these
    /// tests assert the answer space is wider than the pool.
    /// </summary>
    private async Task PublishAsync(
        string transliteration,
        int? shuhlofoNumber,
        CancellationToken ct,
        int modePosition = 1)
    {
        var id = await CreateDraftAsync(transliteration, shuhlofoNumber, ct, modePosition);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[] { 0x53, 0x41, 0x42, 0x52, 0x4F });
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(file, "file", "chant.mp3");

        var uploaded = await client.PostAsync($"/api/v1/admin/chants/{id}/audio", content, ct);
        uploaded.StatusCode.Should().Be(HttpStatusCode.OK);
        uploadedUrls.Add(
            (await uploaded.Content.ReadFromJsonAsync<ChantDto>(SabroApiFactory.JsonOptions, ct))!.AudioUrl!);

        (await client.PostAsync($"/api/v1/admin/chants/{id}/publish", null, ct))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
