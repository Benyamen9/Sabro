using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class LexiconEntriesControllerTests : IDisposable
{
    private const string KtbUnvocalized = "ܟܬܒ";

    private static readonly string[] TwoVariants = { "kthab", "ktab" };

    private static readonly string[] EnFrLanguages = { "en", "fr" };

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public LexiconEntriesControllerTests(PostgresFixture postgres)
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

        var response = await client.PostAsJsonAsync("/api/v1/lexicon-entries", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.SyriacUnvocalized.Should().Be(KtbUnvocalized);
        dto.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/lexicon-entries/{dto.Id}");

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

        var posted = await client.PostAsJsonAsync("/api/v1/lexicon-entries", body, ct);
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

        var response = await client.PostAsync("/api/v1/lexicon-entries", content, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<LexiconEntryDto>(SabroApiFactory.JsonOptions, ct);
        dto!.GrammaticalCategory.Should().Be(GrammaticalCategory.Verb);
    }

    [Fact]
    public async Task Get_OnMissingEntry_Returns404Problem()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync($"/api/v1/lexicon-entries/{Guid.NewGuid()}", ct);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithEmptySyriacUnvocalized_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateLexiconEntryRequest(
            SyriacUnvocalized: string.Empty,
            SblTransliteration: "ktb",
            GrammaticalCategory: GrammaticalCategory.Verb);

        var response = await client.PostAsJsonAsync("/api/v1/lexicon-entries", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("syriacUnvocalized");
    }

    [Fact]
    public async Task Get_List_WithDefaults_Returns200WithPagedShape()
    {
        var ct = TestContext.Current.CancellationToken;
        await client.PostAsJsonAsync(
            "/api/v1/lexicon-entries",
            new CreateLexiconEntryRequest(KtbUnvocalized, "ktb", GrammaticalCategory.Verb),
            ct);

        var response = await client.GetAsync("/api/v1/lexicon-entries", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<LexiconEntryDto>>(SabroApiFactory.JsonOptions, ct);
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Get_List_WithInvalidPaging_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/api/v1/lexicon-entries?page=-1&pageSize=500", ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("page");
        problem.Errors.Should().ContainKey("pageSize");
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
