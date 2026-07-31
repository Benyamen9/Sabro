using System.Net;
using System.Net.Http.Json;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Domain;
using Sabro.IntegrationTests.Api;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Figure descriptions over HTTP and through Postgres: the child table, the
/// full-replacement semantics of the update request, and the rules that must
/// hold at the database boundary rather than only in the aggregate.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AdminHistoricalFigureDescriptionsTests : IDisposable
{
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AdminHistoricalFigureDescriptionsTests(PostgresFixture postgres)
    {
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithDescriptionsInEveryLanguage_RoundTripsThemAll()
    {
        var ct = TestContext.Current.CancellationToken;
        var descriptions = new[]
        {
            new HistoricalFigureDescriptionRequest("en", "A bishop of Edessa and a polymath."),
            new HistoricalFigureDescriptionRequest("fr", "Un évêque d'Édesse et un polymathe."),
            new HistoricalFigureDescriptionRequest("nl", "Een bisschop van Edessa en een polyhistor."),
            new HistoricalFigureDescriptionRequest("de", "Ein Bischof von Edessa und Universalgelehrter."),
            new HistoricalFigureDescriptionRequest("sv", "En biskop av Edessa och en polyhistor."),
        };

        var created = await CreateAsync(ct, descriptions);

        created.Descriptions.Should().HaveCount(5);
        created.Descriptions.Select(d => d.Language)
            .Should().BeEquivalentTo(["en", "fr", "nl", "de", "sv"]);

        // Re-read rather than trusting the create response: this is what proves
        // the child table persisted, not just that the DTO was mapped.
        var reread = await GetAsync(created.Id, ct);
        reread.Descriptions.Single(d => d.Language == "sv").Text
            .Should().Be("En biskop av Edessa och en polyhistor.");
    }

    [Fact]
    public async Task Post_WithNoDescriptions_Succeeds()
    {
        // Descriptions are enrichment. A figure without them is ordinary, not broken.
        var ct = TestContext.Current.CancellationToken;

        var created = await CreateAsync(ct);

        created.Descriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task Put_ReplacesTheWholeSet()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(ct, [
            new HistoricalFigureDescriptionRequest("en", "First."),
            new HistoricalFigureDescriptionRequest("fr", "Première."),
        ]);

        var replacement = new[] { new HistoricalFigureDescriptionRequest("en", "Second.") };

        var updated = await PutAsync(created.Id, replacement, ct);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = (await updated.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct))!;
        dto.Descriptions.Should().ContainSingle();
        dto.Descriptions[0].Text.Should().Be("Second.");

        var reread = await GetAsync(created.Id, ct);
        reread.Descriptions.Should().ContainSingle("the French row must be deleted, not orphaned");
    }

    [Fact]
    public async Task Put_WithoutDescriptions_ClearsThem()
    {
        // Documented in UpdateHistoricalFigureRequest: omitting the field clears
        // them, because every field on that request is a full replacement.
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(ct, [new HistoricalFigureDescriptionRequest("en", "Present.")]);

        await PutAsync(created.Id, null, ct);

        var reread = await GetAsync(created.Id, ct);
        reread.Descriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_WithTwoDescriptionsForOneLanguage_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await PostAsync(ct, [
            new HistoricalFigureDescriptionRequest("en", "First."),
            new HistoricalFigureDescriptionRequest("en", "Second."),
        ]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithAnOverlongDescription_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        var tooLong = new string('a', HistoricalFigureDescription.MaxTextLength + 1);

        var response = await PostAsync(ct, [new HistoricalFigureDescriptionRequest("en", tooLong)]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_RemovesTheDescriptionsWithTheFigure()
    {
        // The child table cascades; a leftover row would break the next figure
        // that happened to reuse the id.
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(ct, [new HistoricalFigureDescriptionRequest("en", "A bishop.")]);

        var deleted = await client.DeleteAsync($"/api/v1/admin/historical-figures/{created.Id}", ct);
        deleted.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);

        var gone = await client.GetAsync($"/api/v1/admin/historical-figures/{created.Id}", ct);
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<HttpResponseMessage> PostAsync(
        CancellationToken ct,
        IReadOnlyList<HistoricalFigureDescriptionRequest>? descriptions = null)
    {
        var body = new CreateHistoricalFigureRequest(
            Name: $"Jacob of Edessa {Guid.NewGuid():N}",
            Category: HistoricalFigureCategory.Patristic,
            Era: 7,
            Period: HistoricalPeriod.PostChalcedonian,
            Role: HistoricalFigureRole.Bishop,
            Region: HistoricalFigureRegion.Syria,
            Gender: HistoricalFigureGender.Male,
            Tradition: HistoricalFigureTradition.WestSyriac,
            Descriptions: descriptions);

        return await client.PostAsJsonAsync("/api/v1/admin/historical-figures", body, ct);
    }

    private async Task<HistoricalFigureDto> CreateAsync(
        CancellationToken ct,
        IReadOnlyList<HistoricalFigureDescriptionRequest>? descriptions = null)
    {
        var response = await PostAsync(ct, descriptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    private async Task<HttpResponseMessage> PutAsync(
        Guid id,
        IReadOnlyList<HistoricalFigureDescriptionRequest>? descriptions,
        CancellationToken ct)
    {
        var body = new UpdateHistoricalFigureRequest(
            Name: $"Jacob of Edessa {Guid.NewGuid():N}",
            Category: HistoricalFigureCategory.Patristic,
            Era: 7,
            Period: HistoricalPeriod.PostChalcedonian,
            Role: HistoricalFigureRole.Bishop,
            Region: HistoricalFigureRegion.Syria,
            Gender: HistoricalFigureGender.Male,
            Tradition: HistoricalFigureTradition.WestSyriac,
            Descriptions: descriptions);

        return await client.PutAsJsonAsync($"/api/v1/admin/historical-figures/{id}", body, ct);
    }

    private async Task<HistoricalFigureDto> GetAsync(Guid id, CancellationToken ct)
    {
        var response = await client.GetAsync($"/api/v1/admin/historical-figures/{id}", ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct))!;
    }
}
