using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Sabro.IntegrationTests.Api;
using Sabro.Shared.Pagination;
using Sabro.Translations.Application.Sources;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class SourcesControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public SourcesControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_Returns201()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = await SeedAuthorAsync(ct);
        var body = new CreateSourceRequest(author.Id, "Commentary on Matthew", "syr", "A description.");

        var response = await client.PostAsJsonAsync("/api/v1/sources", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<SourceDto>(ct);
        dto.Should().NotBeNull();
        dto!.AuthorId.Should().Be(author.Id);
        dto.Title.Should().Be("Commentary on Matthew");
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/sources/{dto.Id}");
    }

    [Fact]
    public async Task Get_OnExistingSource_Returns200WithDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = await SeedAuthorAsync(ct);
        var posted = await client.PostAsJsonAsync(
            "/api/v1/sources",
            new CreateSourceRequest(author.Id, "Get-Test Source", null, null),
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<SourceDto>(ct))!;

        var follow = await client.GetAsync(posted.Headers.Location, ct);

        follow.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await follow.Content.ReadFromJsonAsync<SourceDto>(ct);
        dto!.Id.Should().Be(created.Id);
        dto.Title.Should().Be("Get-Test Source");
    }

    [Fact]
    public async Task Get_OnMissingSource_Returns404Problem()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync($"/api/v1/sources/{Guid.NewGuid()}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithEmptyAuthorId_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateSourceRequest(AuthorId: Guid.Empty, Title: "T", OriginalLanguageCode: null, Description: null);

        var response = await client.PostAsJsonAsync("/api/v1/sources", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("authorId");
    }

    [Fact]
    public async Task Get_List_WithDefaults_Returns200WithPagedShape()
    {
        var ct = TestContext.Current.CancellationToken;
        var author = await SeedAuthorAsync(ct);
        var prefix = $"List-Ctl-Source-{Guid.NewGuid():N}-";
        for (var i = 1; i <= 2; i++)
        {
            var posted = await client.PostAsJsonAsync(
                "/api/v1/sources",
                new CreateSourceRequest(author.Id, $"{prefix}{i}", null, null),
                ct);
            posted.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var response = await client.GetAsync("/api/v1/sources", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<SourceDto>>(ct);
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.Total.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Get_List_WithInvalidPaging_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/api/v1/sources?page=0&pageSize=0", ct);

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

    private async Task<Author> SeedAuthorAsync(CancellationToken ct)
    {
        var author = Author.Create($"Sources-Controller Author {Guid.NewGuid():N}").Value!;
        await using var seed = postgres.CreateContext();
        seed.Authors.Add(author);
        await seed.SaveChangesAsync(ct);
        return author;
    }
}
