using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The public figure roster under <c>/api/v1/historical-figures</c> — anonymous,
/// published-only, and deliberately free of editorial state so the Shmo puzzle
/// pool cannot be enumerated from it.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class HistoricalFiguresControllerTests : IDisposable
{
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public HistoricalFiguresControllerTests(PostgresFixture postgres)
    {
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ListsOnlyPublishedFigures()
    {
        var ct = TestContext.Current.CancellationToken;
        var draftName = $"Draft Figure {Guid.NewGuid():N}";
        var publishedName = $"Published Figure {Guid.NewGuid():N}";

        await CreateAsync(draftName, ct);
        var published = await CreateAsync(publishedName, ct);
        await client.PostAsync($"/api/v1/admin/historical-figures/{published.Id}/publish", content: null, ct);

        var response = await client.GetAsync("/api/v1/historical-figures?pageSize=200", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<HistoricalFigureListItem>>(SabroApiFactory.JsonOptions, ct);
        page!.Items.Select(i => i.Name).Should().Contain(publishedName);
        page.Items.Select(i => i.Name).Should().NotContain(draftName);
    }

    [Fact]
    public async Task Get_PayloadCarriesNoEditorialStateOrPlayableFlag()
    {
        var ct = TestContext.Current.CancellationToken;
        var name = $"Roster Figure {Guid.NewGuid():N}";
        var created = await CreateAsync(name, ct);
        await client.PostAsync($"/api/v1/admin/historical-figures/{created.Id}/publish", content: null, ct);
        await client.PutAsJsonAsync(
            $"/api/v1/admin/historical-figures/{created.Id}/playable",
            new SetPlayableHistoricalFigureRequest(true),
            ct);

        var response = await client.GetAsync($"/api/v1/historical-figures/{created.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var properties = document.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        properties.Should().NotContain("status");
        properties.Should().NotContain("playableInShmo");
        properties.Should().Contain("name");
        properties.Should().Contain("era");
    }

    [Fact]
    public async Task Get_DraftById_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var created = await CreateAsync($"Hidden Draft {Guid.NewGuid():N}", ct);

        var response = await client.GetAsync($"/api/v1/historical-figures/{created.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync($"/api/v1/historical-figures/{Guid.NewGuid()}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HistoricalFigureDto> CreateAsync(string name, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/admin/historical-figures",
            new CreateHistoricalFigureRequest(
                Name: name,
                Category: HistoricalFigureCategory.Patristic,
                Era: 7,
                Period: HistoricalPeriod.PostChalcedonian,
                Role: HistoricalFigureRole.Bishop,
                Region: HistoricalFigureRegion.Syria,
                Gender: HistoricalFigureGender.Male,
                Tradition: HistoricalFigureTradition.WestSyriac),
            ct);
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
