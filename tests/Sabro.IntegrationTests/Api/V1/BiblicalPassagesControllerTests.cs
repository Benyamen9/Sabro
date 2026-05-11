using System.Net;
using System.Net.Http.Json;
using Sabro.Biblical.Application.Books;
using Sabro.Biblical.Application.Passages;
using Sabro.Biblical.Domain;
using Sabro.IntegrationTests.Api;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class BiblicalPassagesControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public BiblicalPassagesControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_FirstCall_Returns201()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = await SeedBookAsync(ct);

        var response = await client.PostAsJsonAsync(
            "/api/v1/biblical-passages",
            new GetOrCreateBiblicalPassageRequest(code, 5, 3),
            SabroApiFactory.JsonOptions,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<BiblicalPassageDto>(SabroApiFactory.JsonOptions, ct);
        dto!.BookCode.Should().Be(code);
        dto.ChapterNumber.Should().Be(5);
        dto.VerseNumber.Should().Be(3);
    }

    [Fact]
    public async Task Post_SecondCallSameReference_Returns200WithSameId()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = await SeedBookAsync(ct);

        var first = await client.PostAsJsonAsync(
            "/api/v1/biblical-passages",
            new GetOrCreateBiblicalPassageRequest(code, 1, 1),
            SabroApiFactory.JsonOptions,
            ct);
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstDto = (await first.Content.ReadFromJsonAsync<BiblicalPassageDto>(SabroApiFactory.JsonOptions, ct))!;

        var second = await client.PostAsJsonAsync(
            "/api/v1/biblical-passages",
            new GetOrCreateBiblicalPassageRequest(code, 1, 1),
            SabroApiFactory.JsonOptions,
            ct);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondDto = (await second.Content.ReadFromJsonAsync<BiblicalPassageDto>(SabroApiFactory.JsonOptions, ct))!;
        secondDto.Id.Should().Be(firstDto.Id);
    }

    [Fact]
    public async Task Post_WithUnknownBook_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.PostAsJsonAsync(
            "/api/v1/biblical-passages",
            new GetOrCreateBiblicalPassageRequest("ZZZNOPE", 1, 1),
            SabroApiFactory.JsonOptions,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_OnExistingPassage_Returns200()
    {
        var ct = TestContext.Current.CancellationToken;
        var code = await SeedBookAsync(ct);
        var posted = await client.PostAsJsonAsync(
            "/api/v1/biblical-passages",
            new GetOrCreateBiblicalPassageRequest(code, 3, 16),
            SabroApiFactory.JsonOptions,
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<BiblicalPassageDto>(SabroApiFactory.JsonOptions, ct))!;

        var response = await client.GetAsync($"/api/v1/biblical-passages/{created.Id}", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<BiblicalPassageDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Id.Should().Be(created.Id);
        dto.BookCode.Should().Be(code);
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
        return $"X{n:D6}";
    }

    private async Task<string> SeedBookAsync(CancellationToken ct)
    {
        var code = NewCode();
        var response = await client.PostAsJsonAsync(
            "/api/v1/biblical-books",
            new CreateBiblicalBookRequest(code, $"Seed {code}", Testament.New, Random.Shared.Next(1, 200)),
            SabroApiFactory.JsonOptions,
            ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return code;
    }
}
