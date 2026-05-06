using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.IntegrationTests.Api;
using Sabro.Lexicon.Application.Roots;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class LexiconRootsControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public LexiconRootsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_Returns201AndPersistsRoot()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateLexiconRootRequest(RandomSyriacRoot());

        var response = await client.PostAsJsonAsync("/api/v1/lexicon-roots", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<LexiconRootDto>(ct);
        dto.Should().NotBeNull();
        dto!.Form.Should().Be(body.Form);
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/lexicon-roots/{dto.Id}");

        await using var ctx = postgres.CreateLexiconContext();
        var loaded = await ctx.Roots.FirstOrDefaultAsync(r => r.Id == dto.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_OnExistingRoot_Returns200WithDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var posted = await client.PostAsJsonAsync(
            "/api/v1/lexicon-roots",
            new CreateLexiconRootRequest(RandomSyriacRoot()),
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<LexiconRootDto>(ct))!;

        var response = await client.GetAsync($"/api/v1/lexicon-roots/{created.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<LexiconRootDto>(ct);
        dto!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Get_OnMissingRoot_Returns404Problem()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync($"/api/v1/lexicon-roots/{Guid.NewGuid()}", ct);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithEmptyForm_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateLexiconRootRequest(string.Empty);

        var response = await client.PostAsJsonAsync("/api/v1/lexicon-roots", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("form");
    }

    [Fact]
    public async Task Get_List_WithDefaults_Returns200WithPagedShape()
    {
        var ct = TestContext.Current.CancellationToken;
        await client.PostAsJsonAsync(
            "/api/v1/lexicon-roots",
            new CreateLexiconRootRequest(RandomSyriacRoot()),
            ct);

        var response = await client.GetAsync("/api/v1/lexicon-roots", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<LexiconRootDto>>(ct);
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Get_List_WithInvalidPaging_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/api/v1/lexicon-roots?page=0&pageSize=999", ct);

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

    private static string RandomSyriacRoot() =>
        new string(new[]
        {
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
            (char)(0x0710 + Random.Shared.Next(22)),
        });
}
