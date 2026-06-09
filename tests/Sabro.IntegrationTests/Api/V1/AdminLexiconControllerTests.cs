using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Owner-only editorial backoffice for the Lexicon under <c>/api/v1/admin/lexicon</c>
/// (the <c>api:v1:admin</c> scope). Covers create, validation, and the
/// draft/publish/playable lifecycle over HTTP.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AdminLexiconControllerTests : IDisposable
{
    private const string KtbUnvocalized = "ܟܬܒ";

    private static readonly string[] TwoVariants = { "kthab", "ktab" };

    private static readonly string[] EnFrLanguages = { "en", "fr" };

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AdminLexiconControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithMinimalPayload_Returns201()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateLexiconEntryRequest(
            SyriacUnvocalized: KtbUnvocalized,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var response = await client.PostAsJsonAsync("/api/v1/admin/lexicon", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        dto.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
        dto.Status.Should().Be(LexiconEntryStatus.Draft);
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/admin/lexicon/{dto.Id}");

        await using var ctx = postgres.CreateLexiconContext();
        var loaded = await ctx.Entries.FirstOrDefaultAsync(e => e.Id == dto.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_WithMeaningsAndVariants_RoundTripsAllFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateLexiconEntryRequest(
            SyriacUnvocalized: KtbUnvocalized,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb,
            TransliterationVariants: TwoVariants,
            Morphology: "Pe'al",
            Meanings: new[]
            {
                new CreateLexiconMeaningRequest("en", "to write"),
                new CreateLexiconMeaningRequest("fr", "écrire"),
            });

        var posted = await client.PostAsJsonAsync("/api/v1/admin/lexicon", body, ct);
        posted.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await posted.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct))!;

        var follow = await client.GetAsync(posted.Headers.Location, ct);
        follow.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await follow.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);

        dto!.Id.Should().Be(created.Id);
        dto.TransliterationVariants.Should().BeEquivalentTo(
            TwoVariants,
            options => options.WithStrictOrdering());
        dto.Morphology.Should().Be("Pe'al");
        dto.Meanings.Select(m => m.Language).Should().BeEquivalentTo(
            EnFrLanguages,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Post_GrammaticalCategoryIsAcceptedAsString()
    {
        var ct = TestContext.Current.CancellationToken;
        var rawJson = $$"""
        {
            "syriacUnvocalized": "{{KtbUnvocalized}}",
            "sblTransliteration": "ktb",
            "grammaticalCategory": "Verb"
        }
        """;
        var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/admin/lexicon", content, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        dto!.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
    }

    [Fact]
    public async Task Post_WithEmptySyriacUnvocalized_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateLexiconEntryRequest(
            SyriacUnvocalized: string.Empty,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var response = await client.PostAsJsonAsync("/api/v1/admin/lexicon", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("syriacUnvocalized");
    }

    [Fact]
    public async Task PublishThenSetPlayable_DrivesLifecycle()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateLexiconEntryRequest(
            SyriacUnvocalized: KtbUnvocalized,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb,
            Meanings: new[]
            {
                new CreateLexiconMeaningRequest("en", "to write"),
                new CreateLexiconMeaningRequest("fr", "écrire"),
                new CreateLexiconMeaningRequest("nl", "schrijven"),
            });

        var created = (await (await client.PostAsJsonAsync("/api/v1/admin/lexicon", body, ct))
            .Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct))!;

        var published = await client.PostAsync($"/api/v1/admin/lexicon/{created.Id}/publish", content: null, ct);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishedDto = await published.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        publishedDto!.Status.Should().Be(LexiconEntryStatus.Published);

        var playable = await client.PutAsJsonAsync(
            $"/api/v1/admin/lexicon/{created.Id}/playable",
            new SetPlayableLexiconEntryRequest(true),
            ct);
        playable.StatusCode.Should().Be(HttpStatusCode.OK);
        var playableDto = await playable.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        playableDto!.PlayableInMeltho.Should().BeTrue();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
