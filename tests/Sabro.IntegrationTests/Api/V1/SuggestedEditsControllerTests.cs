using System.Net;
using System.Net.Http.Json;
using Sabro.Identity.Domain;
using Sabro.Reviews.Application.SuggestedEdits;
using Sabro.Reviews.Domain;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class SuggestedEditsControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public SuggestedEditsControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_AsExpertReviewer_Returns201()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/suggested-edits",
            reviewer,
            body: ValidProposal(),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<SuggestedEditDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Status.Should().Be(SuggestedEditStatus.Pending);
        dto.SubmittedByLogtoUserId.Should().Be(reviewer);
    }

    [Fact]
    public async Task Post_AsReader_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = await SeedProfileAsync(Role.Reader, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/suggested-edits",
            reader,
            body: ValidProposal(),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_OnMissing_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await SendAsync(
            HttpMethod.Get,
            $"/api/v1/suggested-edits/{Guid.NewGuid()}",
            "any-user",
            body: null,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Accept_AsOwner_Returns200AndMarksAccepted()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);
        var owner = await SeedProfileAsync(Role.Owner, ct);

        var posted = await SendAsync(
            HttpMethod.Post,
            "/api/v1/suggested-edits",
            reviewer,
            ValidProposal(),
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<SuggestedEditDto>(SabroApiFactory.JsonOptions, ct))!;

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/suggested-edits/{created.Id}/accept",
            owner,
            new DecisionRequest("Approved."),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<SuggestedEditDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Status.Should().Be(SuggestedEditStatus.Accepted);
        dto.DecisionByLogtoUserId.Should().Be(owner);
    }

    [Fact]
    public async Task Accept_AsReader_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);
        var reader = await SeedProfileAsync(Role.Reader, ct);

        var posted = await SendAsync(
            HttpMethod.Post,
            "/api/v1/suggested-edits",
            reviewer,
            ValidProposal(),
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<SuggestedEditDto>(SabroApiFactory.JsonOptions, ct))!;

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/suggested-edits/{created.Id}/accept",
            reader,
            new DecisionRequest(),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reject_AsOwner_Returns200AndMarksRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);
        var owner = await SeedProfileAsync(Role.Owner, ct);

        var posted = await SendAsync(
            HttpMethod.Post,
            "/api/v1/suggested-edits",
            reviewer,
            ValidProposal(),
            ct);
        var created = (await posted.Content.ReadFromJsonAsync<SuggestedEditDto>(SabroApiFactory.JsonOptions, ct))!;

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/suggested-edits/{created.Id}/reject",
            owner,
            new DecisionRequest("Not consistent with Syriac."),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<SuggestedEditDto>(SabroApiFactory.JsonOptions, ct);
        dto!.Status.Should().Be(SuggestedEditStatus.Rejected);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static CreateSuggestedEditRequest ValidProposal() =>
        new(
            SuggestedEditTargetType.Segment,
            Guid.NewGuid(),
            TargetVersion: 1,
            ProposedContent: "Proposed revised content.");

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
}
