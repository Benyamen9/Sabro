using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Owner-only editorial backoffice for the Shmo roster under
/// <c>/api/v1/admin/historical-figures</c> (the <c>api:v1:admin</c> scope).
/// Covers create, validation, and the draft/publish/playable lifecycle over HTTP.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AdminHistoricalFiguresControllerTests : IDisposable
{
    private const string JacobOfEdessa = "Jacob of Edessa";

    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AdminHistoricalFiguresControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithMinimalPayload_Returns201()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync("/api/v1/admin/historical-figures", NewFigure(), ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.Name.Should().Be(JacobOfEdessa);
        dto.Category.Should().Be(HistoricalFigureCategory.Patristic);
        dto.Era.Should().Be(7);
        dto.Status.Should().Be(HistoricalFigureStatus.Draft);
        dto.PlayableInShmo.Should().BeFalse();
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/admin/historical-figures/{dto.Id}");

        await using var ctx = postgres.CreateHistoricalContext();
        var loaded = await ctx.Figures.FirstOrDefaultAsync(e => e.Id == dto.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_EnumsAreAcceptedAsStrings()
    {
        var ct = TestContext.Current.CancellationToken;
        var rawJson = """
        {
            "name": "Ephrem the Syrian",
            "category": "Patristic",
            "era": 4,
            "role": "Commentator",
            "region": "Mesopotamia",
            "gender": "Male",
            "tradition": "EastSyriac"
        }
        """;
        var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/admin/historical-figures", content, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Role.Should().Be(HistoricalFigureRole.Commentator);
        dto.Region.Should().Be(HistoricalFigureRegion.Mesopotamia);
        dto.Tradition.Should().Be(HistoricalFigureTradition.EastSyriac);
    }

    [Fact]
    public async Task Post_WithEmptyName_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/historical-figures",
            NewFigure(name: string.Empty),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("name");
    }

    [Fact]
    public async Task Post_WithZeroEra_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/historical-figures",
            NewFigure(era: 0),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("era");
    }

    [Fact]
    public async Task Publish_WithoutTradition_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(NewFigure(tradition: null), ct);

        var published = await client.PostAsync(
            $"/api/v1/admin/historical-figures/{created.Id}/publish",
            content: null,
            ct);

        published.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublishThenSetPlayable_DrivesLifecycle()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(NewFigure(), ct);

        var published = await client.PostAsync(
            $"/api/v1/admin/historical-figures/{created.Id}/publish",
            content: null,
            ct);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishedDto = await published.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct);
        publishedDto!.Status.Should().Be(HistoricalFigureStatus.Published);

        var playable = await client.PutAsJsonAsync(
            $"/api/v1/admin/historical-figures/{created.Id}/playable",
            new SetPlayableHistoricalFigureRequest(true),
            ct);
        playable.StatusCode.Should().Be(HttpStatusCode.OK);
        var playableDto = await playable.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct);
        playableDto!.PlayableInShmo.Should().BeTrue();
    }

    [Fact]
    public async Task SetPlayable_OnDraft_Returns409()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(NewFigure(), ct);

        var playable = await client.PutAsJsonAsync(
            $"/api/v1/admin/historical-figures/{created.Id}/playable",
            new SetPlayableHistoricalFigureRequest(true),
            ct);

        playable.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unpublish_ClearsPlayable()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(NewFigure(), ct);
        await client.PostAsync($"/api/v1/admin/historical-figures/{created.Id}/publish", content: null, ct);
        await client.PutAsJsonAsync(
            $"/api/v1/admin/historical-figures/{created.Id}/playable",
            new SetPlayableHistoricalFigureRequest(true),
            ct);

        var unpublished = await client.PostAsync(
            $"/api/v1/admin/historical-figures/{created.Id}/unpublish",
            content: null,
            ct);

        unpublished.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await unpublished.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Status.Should().Be(HistoricalFigureStatus.Draft);
        dto.PlayableInShmo.Should().BeFalse();
    }

    [Fact]
    public async Task Put_ReplacesEditableFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(NewFigure(), ct);

        var updated = await client.PutAsJsonAsync(
            $"/api/v1/admin/historical-figures/{created.Id}",
            new UpdateHistoricalFigureRequest(
                Name: "Ephrem the Syrian",
                Category: HistoricalFigureCategory.Patristic,
                Era: 4,
                Role: HistoricalFigureRole.Commentator,
                Region: HistoricalFigureRegion.Mesopotamia,
                Gender: HistoricalFigureGender.Male,
                Tradition: HistoricalFigureTradition.EastSyriac),
            ct);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await updated.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Name.Should().Be("Ephrem the Syrian");
        dto.Era.Should().Be(4);
        dto.Role.Should().Be(HistoricalFigureRole.Commentator);
    }

    [Fact]
    public async Task Delete_RemovesFigure()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync(NewFigure(), ct);

        var deleted = await client.DeleteAsync($"/api/v1/admin/historical-figures/{created.Id}", ct);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var follow = await client.GetAsync($"/api/v1/admin/historical-figures/{created.Id}", ct);
        follow.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync($"/api/v1/admin/historical-figures/{Guid.NewGuid()}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_FiltersByStatusAndSearch()
    {
        var ct = TestContext.Current.CancellationToken;
        var unique = $"Isaac of Nineveh {Guid.NewGuid():N}";
        var created = await CreateAsync(NewFigure(name: unique), ct);
        await client.PostAsync($"/api/v1/admin/historical-figures/{created.Id}/publish", content: null, ct);

        var response = await client.GetAsync(
            $"/api/v1/admin/historical-figures?search={Uri.EscapeDataString(unique)}&status=Published",
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<HistoricalFigureDto>>(SabroApiFactory.JsonOptions, ct);
        page!.Items.Should().ContainSingle().Which.Name.Should().Be(unique);
    }

    [Fact]
    public async Task List_SearchIsCaseInsensitive()
    {
        var ct = TestContext.Current.CancellationToken;
        var unique = $"Rabbula {Guid.NewGuid():N}";
        await CreateAsync(NewFigure(name: unique), ct);

        var response = await client.GetAsync(
            $"/api/v1/admin/historical-figures?search={Uri.EscapeDataString(unique.ToUpperInvariant())}",
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<HistoricalFigureDto>>(SabroApiFactory.JsonOptions, ct);
        page!.Items.Should().ContainSingle().Which.Name.Should().Be(unique);
    }

    private static CreateHistoricalFigureRequest NewFigure(
        string name = JacobOfEdessa,
        HistoricalFigureCategory category = HistoricalFigureCategory.Patristic,
        int era = 7,
        HistoricalFigureRole role = HistoricalFigureRole.Bishop,
        HistoricalFigureRegion region = HistoricalFigureRegion.Syria,
        HistoricalFigureGender gender = HistoricalFigureGender.Male,
        HistoricalFigureTradition? tradition = HistoricalFigureTradition.WestSyriac) =>
        new(name, category, era, role, region, gender, tradition);

    private async Task<HistoricalFigureDto> CreateAsync(CreateHistoricalFigureRequest request, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/v1/admin/historical-figures", request, ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<HistoricalFigureDto>(SabroApiFactory.JsonOptions, ct))!;
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
