using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.API.Controllers.V1;
using Sabro.IntegrationTests.Api;
using Sabro.Translations.Application.Segments;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class SegmentsControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public SegmentsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_Returns201AsVersionOne()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sourceId, textVersionId) = await SeedSourceAndTextVersionAsync(ct);
        var body = new CreateSegmentRequest(sourceId, ChapterNumber: 1, VerseNumber: 1, textVersionId, "In the beginning.");

        var response = await client.PostAsJsonAsync("/api/v1/segments", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<SegmentDto>(ct);
        dto.Should().NotBeNull();
        dto!.Version.Should().Be(1);
        dto.PreviousVersionId.Should().BeNull();
        response.Headers.Location!.ToString().Should().Be($"/api/v1/segments/{dto.Id}");
    }

    [Fact]
    public async Task Put_OnExistingSegment_Returns200WithVersionTwoLinkedToPredecessor()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sourceId, textVersionId) = await SeedSourceAndTextVersionAsync(ct);
        var created = await client.PostAsJsonAsync(
            "/api/v1/segments",
            new CreateSegmentRequest(sourceId, 7, 3, textVersionId, "Original."),
            ct);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var v1 = (await created.Content.ReadFromJsonAsync<SegmentDto>(ct))!;

        var edited = await client.PutAsJsonAsync(
            $"/api/v1/segments/{v1.Id}",
            new EditSegmentBody("Revised."),
            ct);

        edited.StatusCode.Should().Be(HttpStatusCode.OK);
        var v2 = (await edited.Content.ReadFromJsonAsync<SegmentDto>(ct))!;
        v2.Version.Should().Be(2);
        v2.PreviousVersionId.Should().Be(v1.Id);
        v2.Content.Should().Be("Revised.");

        await using var ctx = postgres.CreateContext();
        var rows = await ctx.Segments
            .Where(s => s.SourceId == sourceId && s.ChapterNumber == 7 && s.VerseNumber == 3)
            .OrderBy(s => s.Version)
            .ToListAsync(ct);
        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Put_OnMissingSegment_Returns404Problem()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.PutAsJsonAsync(
            $"/api/v1/segments/{Guid.NewGuid()}",
            new EditSegmentBody("Anything."),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        problem!.Status.Should().Be(404);
    }

    [Fact]
    public async Task Post_WithEmptyContent_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var (sourceId, textVersionId) = await SeedSourceAndTextVersionAsync(ct);
        var body = new CreateSegmentRequest(sourceId, 1, 1, textVersionId, Content: string.Empty);

        var response = await client.PostAsJsonAsync("/api/v1/segments", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("content");
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string RandomLetterCode() =>
        new string(new[]
        {
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
        });

    private async Task<(Guid SourceId, Guid TextVersionId)> SeedSourceAndTextVersionAsync(CancellationToken ct)
    {
        var author = Author.Create($"Segments-Controller Author {Guid.NewGuid():N}").Value!;
        var source = Source.Create(author.Id, "Some Source").Value!;
        var code = RandomLetterCode();
        var textVersion = TextVersion.Create(code, "ForControllerTest", isRightToLeft: false).Value!;

        await using var seed = postgres.CreateContext();
        seed.Authors.Add(author);
        seed.Sources.Add(source);
        seed.TextVersions.Add(textVersion);
        await seed.SaveChangesAsync(ct);
        return (source.Id, textVersion.Id);
    }
}
