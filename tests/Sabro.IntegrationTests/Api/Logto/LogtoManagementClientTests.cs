using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.API.Logto;

namespace Sabro.IntegrationTests.Api.Logto;

/// <summary>
/// Pure unit tests for the Logto Management client — no Postgres fixture, just a
/// stubbed <see cref="HttpMessageHandler"/>. Covers the unconfigured guard, the
/// token-then-delete happy path, idempotent 404 handling, and error surfacing.
/// </summary>
public class LogtoManagementClientTests
{
    private const string Authority = "https://auth.test/oidc";

    [Fact]
    public async Task DeleteUserAsync_WhenNotConfigured_FailsWithoutCallingLogto()
    {
        var handler = new StubHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var client = NewClient(handler, clientId: string.Empty, clientSecret: string.Empty);

        var result = await client.DeleteUserAsync("logto|abc", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("logto_unconfigured");
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteUserAsync_HappyPath_GetsTokenThenDeletesUser()
    {
        string? deleteUri = null;
        string? deleteAuth = null;
        var handler = new StubHandler((req, _) =>
        {
            if (req.Method == HttpMethod.Post)
            {
                return Json("{\"access_token\":\"tok-123\"}");
            }

            deleteUri = req.RequestUri!.ToString();
            deleteAuth = req.Headers.Authorization?.ToString();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var client = NewClient(handler, clientId: "m2m", clientSecret: "secret");
        var result = await client.DeleteUserAsync("abc123xyz", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        deleteUri.Should().Be("https://auth.test/api/users/abc123xyz");
        deleteAuth.Should().Be("Bearer tok-123");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserAlreadyGone_Treats404AsSuccess()
    {
        var handler = new StubHandler((req, _) => req.Method == HttpMethod.Post
            ? Json("{\"access_token\":\"tok\"}")
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = NewClient(handler, clientId: "m2m", clientSecret: "secret");
        var result = await client.DeleteUserAsync("logto|abc", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_WhenDeleteErrors_Fails()
    {
        var handler = new StubHandler((req, _) => req.Method == HttpMethod.Post
            ? Json("{\"access_token\":\"tok\"}")
            : new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = NewClient(handler, clientId: "m2m", clientSecret: "secret");
        var result = await client.DeleteUserAsync("logto|abc", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("logto_error");
    }

    private static LogtoManagementClient NewClient(HttpMessageHandler handler, string clientId, string clientSecret)
    {
        var http = new HttpClient(handler);
        var options = Options.Create(new LogtoManagementOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Resource = "https://default.logto.app/api",
        });
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Logto:Authority"] = Authority })
            .Build();
        return new LogtoManagementClient(http, options, config, NullLogger<LogtoManagementClient>.Instance);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) =>
            this.responder = responder;

        public List<HttpMethod> Calls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls.Add(request.Method);
            return Task.FromResult(responder(request, cancellationToken));
        }
    }
}
