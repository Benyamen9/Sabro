using System.Security.Claims;

namespace Sabro.API.Configuration;

/// <summary>
/// Chooses the fixed-window rate-limit partition a request is counted against:
/// the authenticated caller when there is one, otherwise the client IP.
/// </summary>
/// <remarks>
/// <para>
/// Extracted from <c>Program.cs</c> so the choice is testable. It was inline and
/// untested, and both halves of it were silently broken in production (see
/// <c>RateLimitPartitioningTests</c> for the regression cases):
/// </para>
/// <para>
/// <b>The user branch never fired.</b> The old key read
/// <c>httpContext.User.Identity?.Name</c>, but <c>UseRateLimiter</c> ran
/// <i>before</i> <c>UseAuthentication</c>, so <c>User</c> was always an
/// unauthenticated principal by the time the key was computed. Even with the
/// order corrected, <c>Identity.Name</c> would still be null: it reads
/// <see cref="ClaimTypes.Name"/>, and a Logto access token carries <c>sub</c>,
/// not <c>name</c>. This reads the same claim pair as
/// <c>ApiControllerBase.ResolveLogtoUserId</c> so "who is the caller" has one
/// answer across the API.
/// </para>
/// <para>
/// <b>The IP branch collapsed into a single bucket.</b> Caddy proxies to
/// <c>api:8080</c> from its own container, so <c>RemoteIpAddress</c> is Caddy's
/// docker IP — the same value for every request on the internet. Without
/// <c>UseForwardedHeaders</c> that made one global 100/min window for the whole
/// ecosystem. The prefixes below also keep a Logto user id that happens to look
/// like an IP from sharing a bucket with that address.
/// </para>
/// </remarks>
public static class RateLimitPartitioning
{
    /// <summary>
    /// Partition for a request with neither an authenticated caller nor a remote
    /// address. Unreachable over a real connection; a shared bucket is the safe
    /// answer if it ever happens, since the alternative is an unlimited one.
    /// </summary>
    public const string UnknownPartition = "unknown";

    public static string ResolvePartitionKey(HttpContext httpContext)
    {
        var logtoUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(logtoUserId))
        {
            return $"user:{logtoUserId}";
        }

        var clientIp = httpContext.Connection.RemoteIpAddress;
        return clientIp is null ? UnknownPartition : $"ip:{clientIp}";
    }
}
