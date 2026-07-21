using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sabro.API.Controllers.V1;
using Sabro.API.Logto;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Play.Application.GameResults;
using Sabro.Shared.Results;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(IntegrationCollection.Name)]
public class ProfileControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public ProfileControllerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_OnFirstCall_AutoCreatesProfileForTestUser()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();

        var response = await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.LogtoUserId.Should().Be(testUser);
        dto.PreferredLanguage.Should().Be("en");
        dto.PreferredScriptVariant.Should().Be(ScriptVariant.Serto);

        await using var ctx = postgres.CreateIdentityContext();
        var loaded = await ctx.UserProfiles.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMe_OnSecondCall_ReturnsSameProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();

        var first = await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct);
        var firstDto = (await first.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct))!;

        var second = await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct);
        var secondDto = (await second.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct))!;

        secondDto.Id.Should().Be(firstDto.Id);
        secondDto.CreatedAt.Should().BeCloseTo(firstDto.CreatedAt, precision: TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task PutMe_OnExistingProfile_UpdatesPreferences()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();
        await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct);

        var body = new UpdateUserProfileRequest("fr", ScriptVariant.Serto);
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/me")
        {
            Content = JsonContent.Create(body, options: SabroApiFactory.JsonOptions),
        };
        request.Headers.Add(TestAuthHandler.UserHeaderName, testUser);
        var response = await client.SendAsync(request, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct);
        dto!.PreferredLanguage.Should().Be("fr");
        dto.PreferredScriptVariant.Should().Be(ScriptVariant.Serto);
    }

    [Fact]
    public async Task PutMe_WithUnsupportedLanguage_Returns400WithFieldError()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();
        await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct);

        // "de" is a real supported language (see SupportedLanguagesOptions); "xx" is not.
        var rawJson = """
        {
            "preferredLanguage": "xx",
            "preferredScriptVariant": "Serto"
        }
        """;
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/me")
        {
            Content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(TestAuthHandler.UserHeaderName, testUser);
        var response = await client.SendAsync(request, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("preferredLanguage");
    }

    [Fact]
    public async Task ExportMe_WithProfileAndResults_ReturnsCompletePersonalData()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();

        await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct); // auto-creates the profile
        await SeedGameResultAsync(testUser, ct);

        var response = await GetAsTestUserAsync("/api/v1/profile/me/export", testUser, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var export = await response.Content.ReadFromJsonAsync<ProfileExportDto>(SabroApiFactory.JsonOptions, ct);
        export.Should().NotBeNull();
        export!.Profile.LogtoUserId.Should().Be(testUser);
        export.GameResults.Should().ContainSingle().Which.LogtoUserId.Should().Be(testUser);
        export.Scope.Should().NotBeNullOrWhiteSpace();
        export.ExportedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ExportMe_ForNewUser_ReturnsProfileAndEmptyResults()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();

        var response = await GetAsTestUserAsync("/api/v1/profile/me/export", testUser, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var export = await response.Content.ReadFromJsonAsync<ProfileExportDto>(SabroApiFactory.JsonOptions, ct);
        export!.Profile.LogtoUserId.Should().Be(testUser);
        export.GameResults.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMe_WithGameResultsAndProfile_ErasesBothAndTheLogtoIdentity()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();
        var fakeLogto = new FakeLogtoManagementClient(Result<bool>.Success(true));

        await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct); // auto-creates the profile
        await SeedGameResultAsync(testUser, ct);

        using var deletingFactory = WithFakeLogto(fakeLogto);
        using var deletingClient = deletingFactory.CreateClient();
        var response = await SendDeleteAsync(deletingClient, testUser, ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        fakeLogto.DeletedUserIds.Should().ContainSingle().Which.Should().Be(testUser);

        await using var identity = postgres.CreateIdentityContext();
        (await identity.UserProfiles.AnyAsync(p => p.LogtoUserId == testUser, ct)).Should().BeFalse();

        await using var play = postgres.CreatePlayContext();
        (await play.GameResults.AnyAsync(r => r.LogtoUserId == testUser, ct)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMe_WhenLogtoIdentityDeletionFails_StillErasesSabroDataFirst()
    {
        // Documents the intentional order in ProfileController.DeleteMe: Sabro data
        // (game results, profile) is erased before the Logto call, so a failure at
        // the identity-provider step never leaves Sabro-side data behind — only the
        // (retryable) identity deletion is left outstanding.
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();
        var fakeLogto = new FakeLogtoManagementClient(Result<bool>.Failure(new Error("logto_error", "boom")));

        await GetAsTestUserAsync("/api/v1/profile/me", testUser, ct);
        await SeedGameResultAsync(testUser, ct);

        using var deletingFactory = WithFakeLogto(fakeLogto);
        using var deletingClient = deletingFactory.CreateClient();
        var response = await SendDeleteAsync(deletingClient, testUser, ct);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        await using var identity = postgres.CreateIdentityContext();
        (await identity.UserProfiles.AnyAsync(p => p.LogtoUserId == testUser, ct)).Should().BeFalse();

        await using var play = postgres.CreatePlayContext();
        (await play.GameResults.AnyAsync(r => r.LogtoUserId == testUser, ct)).Should().BeFalse();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string NewTestUser() => $"test-user-{Guid.NewGuid():N}";

    private static async Task<HttpResponseMessage> SendDeleteAsync(HttpClient httpClient, string testUser, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/profile/me");
        request.Headers.Add(TestAuthHandler.UserHeaderName, testUser);
        return await httpClient.SendAsync(request, ct);
    }

    private async Task<HttpResponseMessage> GetAsTestUserAsync(string url, string testUser, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(TestAuthHandler.UserHeaderName, testUser);
        return await client.SendAsync(request, ct);
    }

    private async Task SeedGameResultAsync(string testUser, CancellationToken ct)
    {
        await using var ctx = postgres.CreatePlayContext();
        var service = new GameResultService(ctx, new RecordGameResultRequestValidator(), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<GameResultService>.Instance);
        await service.RecordAsync(testUser, new RecordGameResultRequest("meltho", new DateOnly(2026, 7, 1), true, 3, null), ct);
    }

    private WebApplicationFactory<Program> WithFakeLogto(ILogtoManagementClient fakeLogto) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped(_ => fakeLogto)));

    private sealed class FakeLogtoManagementClient : ILogtoManagementClient
    {
        private readonly Result<bool> result;

        public FakeLogtoManagementClient(Result<bool> result) => this.result = result;

        public List<string> DeletedUserIds { get; } = new();

        public Task<Result<bool>> DeleteUserAsync(string logtoUserId, CancellationToken cancellationToken)
        {
            DeletedUserIds.Add(logtoUserId);
            return Task.FromResult(result);
        }
    }
}
