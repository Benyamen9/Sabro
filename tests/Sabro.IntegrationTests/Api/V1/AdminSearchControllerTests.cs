using System.Net;
using System.Net.Http.Json;
using Sabro.API.Controllers.V1;
using Sabro.Identity.Domain;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(TranslationsCollection.Name)]
public class AdminSearchControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly MeilisearchFixture meili;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public AdminSearchControllerTests(PostgresFixture postgres, MeilisearchFixture meili)
    {
        this.postgres = postgres;
        this.meili = meili;
        factory = new SabroApiFactory(postgres.ConnectionString, meili.Url);
        client = factory.CreateClient();
    }

    [Theory]
    [InlineData("lexicon")]
    [InlineData("translations")]
    [InlineData("annotations")]
    [InlineData("biblical_passages")]
    public async Task Rebuild_AsOwner_Returns200WithResult(string indexName)
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/search/rebuild/{indexName}", owner, body: null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<SearchRebuildResponse>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.IndexName.Should().Be(indexName);
        dto.DocumentCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Rebuild_AsReader_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = await SeedProfileAsync(Role.Reader, ct);

        var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/search/rebuild/lexicon", reader, body: null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rebuild_AsExpertReviewer_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;
        var reviewer = await SeedProfileAsync(Role.ExpertReviewer, ct);

        var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/search/rebuild/lexicon", reviewer, body: null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rebuild_OnUnknownIndex_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/search/rebuild/does-not-exist", owner, body: null, ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RepublishAnnotationApprovals_AsOwner_Returns200()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await SeedProfileAsync(Role.Owner, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/admin/search/republish-annotation-approvals",
            owner,
            body: null,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AnnotationApprovalRepublishResponse>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.AnnotationCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RepublishAnnotationApprovals_AsReader_Returns403()
    {
        var ct = TestContext.Current.CancellationToken;
        var reader = await SeedProfileAsync(Role.Reader, ct);

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/admin/search/republish-annotation-approvals",
            reader,
            body: null,
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<string> SeedProfileAsync(Role role, CancellationToken ct)
    {
        var logtoUserId = $"admin-test-{Guid.NewGuid():N}";
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
