using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.IntegrationTests.Api;
using Sabro.Shared.Pagination;
using Sabro.Translations.Application.Authors;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class AuthorsControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AuthorsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_Returns201AndPersistsAuthor()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateAuthorRequest("Controller-Test Author", "ܛܣܛܐ", "A title");

        var response = await client.PostAsJsonAsync("/api/v1/authors", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<AuthorDto>(ct);
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("Controller-Test Author");
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/authors/{dto.Id}");

        await using var ctx = postgres.CreateContext();
        var loaded = await ctx.Authors.FirstOrDefaultAsync(a => a.Id == dto.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_OnExistingAuthor_Returns200WithDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var posted = await client.PostAsJsonAsync(
            "/api/v1/authors",
            new CreateAuthorRequest("Get-Test Author", null, null),
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<AuthorDto>(ct))!;

        var response = await client.GetAsync($"/api/v1/authors/{created.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AuthorDto>(ct);
        dto!.Id.Should().Be(created.Id);
        dto.Name.Should().Be("Get-Test Author");
    }

    [Fact]
    public async Task Get_OnMissingAuthor_Returns404Problem()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync($"/api/v1/authors/{Guid.NewGuid()}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostThenFollowLocation_RoundTripsTheSameAuthor()
    {
        var ct = TestContext.Current.CancellationToken;
        var posted = await client.PostAsJsonAsync(
            "/api/v1/authors",
            new CreateAuthorRequest("Roundtrip Author", null, null),
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<AuthorDto>(ct))!;
        var location = posted.Headers.Location!;

        var follow = await client.GetAsync(location, ct);
        follow.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await follow.Content.ReadFromJsonAsync<AuthorDto>(ct);
        dto!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Post_WithEmptyName_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateAuthorRequest(Name: string.Empty, SyriacName: null, Title: null);

        var response = await client.PostAsJsonAsync("/api/v1/authors", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("name");
        problem.Errors["name"].Should().NotBeEmpty();
    }

    [Fact]
    public async Task Post_WithLatinSyriacName_Returns400FromDomainLayer()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateAuthorRequest(Name: "Author", SyriacName: "Latin", Title: null);

        var response = await client.PostAsJsonAsync("/api/v1/authors", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_List_WithDefaults_Returns200WithPagedShape()
    {
        var ct = TestContext.Current.CancellationToken;
        var prefix = $"List-Ctl-{Guid.NewGuid():N}-";
        for (var i = 1; i <= 2; i++)
        {
            var posted = await client.PostAsJsonAsync(
                "/api/v1/authors",
                new CreateAuthorRequest($"{prefix}{i}", null, null),
                ct);
            posted.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var response = await client.GetAsync("/api/v1/authors", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AuthorDto>>(ct);
        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.Total.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Get_List_WithExplicitPaging_EchoesPageAndPageSize()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/api/v1/authors?page=2&pageSize=5", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AuthorDto>>(ct);
        page!.Page.Should().Be(2);
        page.PageSize.Should().Be(5);
        page.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task Get_List_WithInvalidPaging_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/api/v1/authors?page=0&pageSize=999", ct);

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
