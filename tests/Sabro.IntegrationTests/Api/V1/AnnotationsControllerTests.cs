using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.API.Controllers.V1;
using Sabro.IntegrationTests.Api;
using Sabro.Shared.Pagination;
using Sabro.Translations.Application.Annotations;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class AnnotationsControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AnnotationsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidPayload_Returns201AsVersionOne()
    {
        var ct = TestContext.Current.CancellationToken;
        var segmentId = await SeedSegmentAsync(ct);
        var body = new CreateAnnotationRequest(segmentId, AnchorStart: 4, AnchorEnd: 11, Body: "the Logos");

        var response = await client.PostAsJsonAsync("/api/v1/annotations", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<AnnotationDto>(ct);
        dto!.Version.Should().Be(1);
        dto.PreviousVersionId.Should().BeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/annotations/{dto.Id}");
    }

    [Fact]
    public async Task Put_OnExistingAnnotation_Returns200WithVersionTwoLinkedToPredecessor()
    {
        var ct = TestContext.Current.CancellationToken;
        var segmentId = await SeedSegmentAsync(ct);
        var posted = await client.PostAsJsonAsync(
            "/api/v1/annotations",
            new CreateAnnotationRequest(segmentId, 0, 5, "v1 body"),
            ct);
        var v1 = (await posted.Content.ReadFromJsonAsync<AnnotationDto>(ct))!;

        var edited = await client.PutAsJsonAsync(
            $"/api/v1/annotations/{v1.Id}",
            new EditAnnotationBody("v2 body"),
            ct);

        edited.StatusCode.Should().Be(HttpStatusCode.OK);
        var v2 = (await edited.Content.ReadFromJsonAsync<AnnotationDto>(ct))!;
        v2.Version.Should().Be(2);
        v2.PreviousVersionId.Should().Be(v1.Id);
        v2.Body.Should().Be("v2 body");

        await using var ctx = postgres.CreateContext();
        var rows = await ctx.Annotations
            .Where(a => a.SegmentId == segmentId && a.AnchorStart == 0 && a.AnchorEnd == 5)
            .OrderBy(a => a.Version)
            .ToListAsync(ct);
        rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Put_OnMissingAnnotation_Returns404Problem()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.PutAsJsonAsync(
            $"/api/v1/annotations/{Guid.NewGuid()}",
            new EditAnnotationBody("Anything."),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_OnExistingAnnotation_Returns200WithDto()
    {
        var ct = TestContext.Current.CancellationToken;
        var segmentId = await SeedSegmentAsync(ct);
        var posted = await client.PostAsJsonAsync(
            "/api/v1/annotations",
            new CreateAnnotationRequest(segmentId, 2, 8, "Get-Test body"),
            ct);

        var follow = await client.GetAsync(posted.Headers.Location, ct);

        follow.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await follow.Content.ReadFromJsonAsync<AnnotationDto>(ct);
        dto!.Body.Should().Be("Get-Test body");
        dto.AnchorStart.Should().Be(2);
        dto.AnchorEnd.Should().Be(8);
    }

    [Fact]
    public async Task Post_WithEmptyBody_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var segmentId = await SeedSegmentAsync(ct);
        var body = new CreateAnnotationRequest(segmentId, 0, 5, Body: string.Empty);

        var response = await client.PostAsJsonAsync("/api/v1/annotations", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("body");
    }

    [Fact]
    public async Task Get_List_WithDefaults_Returns200WithPagedShape()
    {
        var ct = TestContext.Current.CancellationToken;
        var segmentId = await SeedSegmentAsync(ct);
        var marker = $"List-Ctl-Annot-{Guid.NewGuid():N}";
        for (var i = 1; i <= 2; i++)
        {
            var posted = await client.PostAsJsonAsync(
                "/api/v1/annotations",
                new CreateAnnotationRequest(segmentId, i * 2, (i * 2) + 4, $"{marker}-{i}"),
                ct);
            posted.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var response = await client.GetAsync("/api/v1/annotations", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AnnotationDto>>(ct);
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(50);
        page.Total.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Get_List_WithInvalidPaging_Returns400ProblemWithFieldErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await client.GetAsync("/api/v1/annotations?page=-1&pageSize=500", ct);

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

    private static string RandomLetterCode() =>
        new string(new[]
        {
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
            (char)('a' + Random.Shared.Next(26)),
        });

    private async Task<Guid> SeedSegmentAsync(CancellationToken ct)
    {
        var author = Author.Create($"Annot-Controller Author {Guid.NewGuid():N}").Value!;
        var source = Source.Create(author.Id, "Some Source").Value!;
        var textVersion = TextVersion.Create(RandomLetterCode(), "ForAnnotControllerTest", isRightToLeft: false).Value!;
        var segment = Segment.Create(source.Id, 1, 1, textVersion.Id, "Segment content for annotation tests.").Value!;

        await using var seed = postgres.CreateContext();
        seed.Authors.Add(author);
        seed.Sources.Add(source);
        seed.TextVersions.Add(textVersion);
        seed.Segments.Add(segment);
        await seed.SaveChangesAsync(ct);
        return segment.Id;
    }
}
