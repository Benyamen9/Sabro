using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(IntegrationCollection.Name)]
public class UsersControllerTests : IDisposable
{
    private readonly PostgresFixture postgres;
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public UsersControllerTests(PostgresFixture postgres)
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

        var response = await GetAsTestUserAsync("/api/v1/users/me", testUser, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct);
        dto.Should().NotBeNull();
        dto!.LogtoUserId.Should().Be(testUser);
        dto.PreferredLanguage.Should().Be("en");
        dto.PreferredScriptVariant.Should().Be(ScriptVariant.Estrangela);

        await using var ctx = postgres.CreateIdentityContext();
        var loaded = await ctx.UserProfiles.FirstOrDefaultAsync(p => p.Id == dto.Id, ct);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMe_OnSecondCall_ReturnsSameProfile()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();

        var first = await GetAsTestUserAsync("/api/v1/users/me", testUser, ct);
        var firstDto = (await first.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct))!;

        var second = await GetAsTestUserAsync("/api/v1/users/me", testUser, ct);
        var secondDto = (await second.Content.ReadFromJsonAsync<UserProfileDto>(SabroApiFactory.JsonOptions, ct))!;

        secondDto.Id.Should().Be(firstDto.Id);
        secondDto.CreatedAt.Should().BeCloseTo(firstDto.CreatedAt, precision: TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task PatchMe_OnExistingProfile_UpdatesPreferences()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();
        await GetAsTestUserAsync("/api/v1/users/me", testUser, ct);

        var body = new UpdateUserProfileRequest("fr", ScriptVariant.Serto);
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/users/me")
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
    public async Task PatchMe_WithUnsupportedLanguage_Returns400WithFieldError()
    {
        var ct = TestContext.Current.CancellationToken;
        var testUser = NewTestUser();
        await GetAsTestUserAsync("/api/v1/users/me", testUser, ct);

        var rawJson = """
        {
            "preferredLanguage": "de",
            "preferredScriptVariant": "Serto"
        }
        """;
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/users/me")
        {
            Content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(TestAuthHandler.UserHeaderName, testUser);
        var response = await client.SendAsync(request, ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ct);
        problem!.Errors.Should().ContainKey("preferredLanguage");
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string NewTestUser() => $"test-user-{Guid.NewGuid():N}";

    private async Task<HttpResponseMessage> GetAsTestUserAsync(string url, string testUser, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(TestAuthHandler.UserHeaderName, testUser);
        return await client.SendAsync(request, ct);
    }
}
