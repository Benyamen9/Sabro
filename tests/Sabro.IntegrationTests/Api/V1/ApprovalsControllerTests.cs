using System.Net;
using System.Net.Http.Json;
using Sabro.Identity.Domain;
using Sabro.Reviews.Application.Approvals;
using Sabro.Reviews.Domain;
using Sabro.Translations.Domain;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class ApprovalsControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public ApprovalsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_AsOwner_Returns201()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            owner,
            ValidSegmentRequest(),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ApprovalDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Status.Should().Be(ApprovalStatus.Approved);
        dto.DecisionByLogtoUserId.Should().Be(owner);
    }

    [Fact]
    public async Task Post_AsReader_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = await SeedProfileAsync(Role.Reader, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            reader,
            ValidSegmentRequest(),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_AsExpertReviewer_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            reviewer,
            ValidSegmentRequest(),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_OnMissing_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/approvals/{Guid.NewGuid()}",
            "any-user",
            body: null,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEffective_ReturnsLatestChapterAndVerseRows()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceId = Guid.NewGuid();

        var chapter = await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            owner,
            new CreateApprovalRequest(
                ApprovalTargetType.Chapter,
                sourceId,
                ChapterNumber: 2,
                VerseNumber: null,
                Version: null,
                AnnotationId: null,
                ApprovalStatus.Approved),
            ct);
        chapter.StatusCode.Should().Be(HttpStatusCode.Created);

        var verse = await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            owner,
            new CreateApprovalRequest(
                ApprovalTargetType.Segment,
                sourceId,
                ChapterNumber: 2,
                VerseNumber: 5,
                Version: 1,
                AnnotationId: null,
                ApprovalStatus.Rejected),
            ct);
        verse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/approvals/effective?sourceId={sourceId}&chapter=2",
            "any-user",
            body: null,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<EffectiveChapterApprovalsDto>(SabroApiFactory.JsonOptions, ct);
        dto!.ChapterApproval.Should().NotBeNull();
        dto.ChapterApproval!.Status.Should().Be(ApprovalStatus.Approved);
        dto.VerseApprovals.Should().HaveCount(1);
        dto.VerseApprovals.Single().Status.Should().Be(ApprovalStatus.Rejected);
        dto.VerseApprovals.Single().VerseNumber.Should().Be(5);
    }

    [Fact]
    public async Task Post_Annotation_AsOwner_DenormalizesParentLocator()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var seed = await SeedAnnotationAsync(chapter: 7, verse: 4, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            owner,
            new CreateApprovalRequest(
                ApprovalTargetType.Annotation,
                SourceId: null,
                ChapterNumber: null,
                VerseNumber: null,
                Version: null,
                AnnotationId: seed.AnnotationId,
                ApprovalStatus.Approved,
                Note: "Annotation reviewed."),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ApprovalDto>(SabroApiFactory.JsonOptions, ct);
        dto!.TargetType.Should().Be(ApprovalTargetType.Annotation);
        dto.AnnotationId.Should().Be(seed.AnnotationId);
        dto.SourceId.Should().Be(seed.SourceId);
        dto.ChapterNumber.Should().Be(7);
        dto.VerseNumber.Should().Be(4);
        dto.Version.Should().Be(seed.AnnotationVersion);
    }

    [Fact]
    public async Task List_FiltersByTargetType()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);
        var sourceId = Guid.NewGuid();

        await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            owner,
            new CreateApprovalRequest(ApprovalTargetType.Chapter, sourceId, 1, null, null, null, ApprovalStatus.Approved),
            ct);
        await SendAsync(
            HttpMethod.Post,
            "/api/v1/approvals",
            owner,
            new CreateApprovalRequest(ApprovalTargetType.Segment, sourceId, 1, 1, 1, null, ApprovalStatus.Approved),
            ct);

        var response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/approvals?sourceId={sourceId}&targetType={ApprovalTargetType.Chapter}",
            "any-user",
            body: null,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<Sabro.Shared.Pagination.PagedResult<ApprovalDto>>(SabroApiFactory.JsonOptions, ct);
        page!.Total.Should().Be(1);
        page.Items.Single().TargetType.Should().Be(ApprovalTargetType.Chapter);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static CreateApprovalRequest ValidSegmentRequest() =>
        new(
            ApprovalTargetType.Segment,
            Guid.NewGuid(),
            ChapterNumber: 1,
            VerseNumber: 1,
            Version: 1,
            AnnotationId: null,
            ApprovalStatus.Approved);

    private static string RandomLetterCode()
    {
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        var rng = Random.Shared;
        Span<char> buffer = stackalloc char[3];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = letters[rng.Next(letters.Length)];
        }

        return new string(buffer);
    }

    private async Task<string> SeedProfileAsync(Role role, CancellationToken ct)
    {
        var logtoUserId = $"test-user-{Guid.NewGuid():N}";
        await using var ctx = postgres.CreateIdentityContext();
        var profile = UserProfile.Create(logtoUserId).Value!;
        profile.AssignRole(role);
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync(ct);
        return logtoUserId;
    }

    private async Task<AnnotationSeed> SeedAnnotationAsync(int chapter, int verse, CancellationToken ct)
    {
        var author = Author.Create($"Author-{Guid.NewGuid():N}").Value!;
        var source = Source.Create(author.Id, $"Source-{Guid.NewGuid():N}").Value!;
        var textVersion = TextVersion.Create(RandomLetterCode(), $"Tv-{Guid.NewGuid():N}", isRightToLeft: false).Value!;
        var segment = Segment.Create(source.Id, chapter, verse, textVersion.Id, "Hello world!").Value!;
        var annotation = Annotation.Create(segment.Id, 0, 5, "Note body.").Value!;

        await using var write = postgres.CreateContext();
        write.Authors.Add(author);
        write.Sources.Add(source);
        write.TextVersions.Add(textVersion);
        write.Segments.Add(segment);
        write.Annotations.Add(annotation);
        await write.SaveChangesAsync(ct);

        return new AnnotationSeed(annotation.Id, annotation.Version, source.Id);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string testUser,
        object? body,
        CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TestAuthHandler.UserHeaderName, testUser);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: SabroApiFactory.JsonOptions);
        }

        return await client.SendAsync(request, ct);
    }

    private sealed record AnnotationSeed(Guid AnnotationId, int AnnotationVersion, Guid SourceId);
}
