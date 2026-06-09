using Microsoft.AspNetCore.Authorization;

namespace Sabro.API.Configuration;

public static class AuthPolicies
{
    public const string Read = "api:v1:read";

    public const string Write = "api:v1:write";

    public const string Admin = "api:v1:admin";

    /// <summary>
    /// True when the caller's token grants <paramref name="scope"/>. OIDC issues
    /// the granted scopes as a single space-delimited <c>scope</c> claim
    /// (e.g. <c>"api:v1:read api:v1:admin"</c>), so an exact-value claim match
    /// would never succeed for a multi-scope token. This splits every
    /// <c>scope</c> claim on spaces and checks membership, tolerating both the
    /// space-delimited form and one-claim-per-scope.
    /// </summary>
    public static bool HasScope(AuthorizationHandlerContext context, string scope) =>
        context.User
            .FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope);
}
