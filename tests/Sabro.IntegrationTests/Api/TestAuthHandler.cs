using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sabro.IntegrationTests.Api;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    /// <summary>
    /// Tests that need to isolate per-user state (e.g. Identity's auto-create
    /// behavior) can set this header on the request to override the default
    /// <c>sub</c> claim; when absent, the historical default is used so
    /// pre-existing tests are unaffected.
    /// </summary>
    public const string UserHeaderName = "X-Test-User";

    private const string DefaultUser = "integration-test-user";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = Request.Headers.TryGetValue(UserHeaderName, out var override_) && !string.IsNullOrWhiteSpace(override_)
            ? override_.ToString()
            : DefaultUser;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user),
            new Claim(ClaimTypes.NameIdentifier, user),
            new Claim("scope", "api:v1:read"),
            new Claim("scope", "api:v1:write"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
