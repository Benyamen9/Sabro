using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Sabro.Biblical.Application.Books;
using Sabro.Biblical.Domain;
using Sabro.IntegrationTests.Api;
using Sabro.Shared.Pagination;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class BiblicalBooksControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public BiblicalBooksControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_Returns201()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = NewCode();
        var body = new CreateBiblicalBookRequest(code, "Matthew", Testament.New, 40, "ܡܬܝ");

        var response = await client.PostAsJsonAsync("/api/v1/biblical-books", body, SabroApiFactory.JsonOptions, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<BiblicalBookDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Code.Should().Be(code);
        dto.Testament.Should().Be(Testament.New);
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/biblical-books/{dto.Id}");
    }

    [Fact]
    public async Task Post_WithDuplicateCode_Returns409Conflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = NewCode();
        var first = await client.PostAsJsonAsync(
            "/api/v1/biblical-books",
            new CreateBiblicalBookRequest(code, "Matthew", Testament.New, 40),
            SabroApiFactory.JsonOptions,
            ct);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(
            "/api/v1/biblical-books",
            new CreateBiblicalBookRequest(code, "Other", Testament.New, 41),
            SabroApiFactory.JsonOptions,
            ct);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetByCode_NormalizesInputToUpper()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = NewCode();
        await client.PostAsJsonAsync(
            "/api/v1/biblical-books",
            new CreateBiblicalBookRequest(code, "Matthew", Testament.New, 40),
            SabroApiFactory.JsonOptions,
            ct);

        var response = await client.GetAsync($"/api/v1/biblical-books/by-code/{code.ToLowerInvariant()}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BiblicalBookDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Code.Should().Be(code);
    }

    [Fact]
    public async Task Get_OnMissingBook_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync($"/api/v1/biblical-books/{Guid.NewGuid()}", ct);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_WithMissingEnglishName_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        var body = new CreateBiblicalBookRequest(NewCode(), string.Empty, Testament.New, 40);

        var response = await client.PostAsJsonAsync("/api/v1/biblical-books", body, SabroApiFactory.JsonOptions, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("englishName");
    }

    [Fact]
    public async Task Get_List_WithDefaults_ReturnsPagedShape()
    {
        var ct = TestContext.Current.CancellationToken;
        await client.PostAsJsonAsync(
            "/api/v1/biblical-books",
            new CreateBiblicalBookRequest(NewCode(), "List-Test", Testament.New, 99),
            SabroApiFactory.JsonOptions,
            ct);

        var response = await client.GetAsync("/api/v1/biblical-books", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<BiblicalBookDto>>(SabroApiFactory.JsonOptions, ct);
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.Total.Should().BeGreaterThanOrEqualTo(1);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string NewCode()
    {
        var n = Random.Shared.Next(0, 1_000_000);
        return $"C{n:D6}";
    }
}
