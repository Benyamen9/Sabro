using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sabro.API.Configuration;

namespace Sabro.UnitTests.Api;

/// <summary>
/// Guards the rate-limit partition key. Until 2026-09-28 the API had a limiter
/// that looked correct and partitioned nothing: every request in the ecosystem
/// shared one 100/min window. Nothing in the test suite asserted any of this,
/// which is why it survived a live security audit that recorded the limit as
/// "100/min per IP".
/// </summary>
public sealed class RateLimitPartitioningTests
{
    [Fact]
    public void ResolvePartitionKey_PrefersTheAuthenticatedCaller()
    {
        var context = ContextFor(logtoUserId: "logto-abc", remoteIp: "203.0.113.7");

        RateLimitPartitioning.ResolvePartitionKey(context).Should().Be("user:logto-abc");
    }

    /// <summary>
    /// A Logto access token carries <c>sub</c>, not <c>name</c>. The old key read
    /// <c>User.Identity.Name</c>, which is backed by <see cref="ClaimTypes.Name"/>
    /// and would have stayed null even once the middleware order was corrected.
    /// </summary>
    [Fact]
    public void ResolvePartitionKey_ReadsTheSubClaim_NotTheNameClaim()
    {
        var identity = new ClaimsIdentity([new Claim("sub", "logto-xyz")], "Test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

        context.User.Identity!.Name.Should().BeNull("a Logto token carries no name claim");
        RateLimitPartitioning.ResolvePartitionKey(context).Should().Be("user:logto-xyz");
    }

    [Fact]
    public void ResolvePartitionKey_FallsBackToTheClientIp_WhenAnonymous()
    {
        var context = ContextFor(logtoUserId: null, remoteIp: "203.0.113.7");

        RateLimitPartitioning.ResolvePartitionKey(context).Should().Be("ip:203.0.113.7");
    }

    /// <summary>
    /// The regression that mattered in production: behind Caddy every request
    /// carried the same <c>RemoteIpAddress</c>, so two unrelated visitors shared
    /// a bucket. With <c>UseForwardedHeaders</c> restoring the real address, they
    /// must not.
    /// </summary>
    [Fact]
    public void ResolvePartitionKey_SeparatesTwoAnonymousClients()
    {
        var first = RateLimitPartitioning.ResolvePartitionKey(ContextFor(null, "203.0.113.7"));
        var second = RateLimitPartitioning.ResolvePartitionKey(ContextFor(null, "198.51.100.4"));

        first.Should().NotBe(second);
    }

    [Fact]
    public void ResolvePartitionKey_SeparatesTwoAuthenticatedCallers()
    {
        var first = RateLimitPartitioning.ResolvePartitionKey(ContextFor("logto-abc", "203.0.113.7"));
        var second = RateLimitPartitioning.ResolvePartitionKey(ContextFor("logto-def", "203.0.113.7"));

        first.Should().NotBe(second);
    }

    /// <summary>
    /// A user id that looks like an address must not land in that address's bucket.
    /// </summary>
    [Fact]
    public void ResolvePartitionKey_DoesNotLetAUserIdCollideWithAnIp()
    {
        var user = RateLimitPartitioning.ResolvePartitionKey(ContextFor("203.0.113.7", "198.51.100.4"));
        var ip = RateLimitPartitioning.ResolvePartitionKey(ContextFor(null, "203.0.113.7"));

        user.Should().NotBe(ip);
    }

    [Fact]
    public void ResolvePartitionKey_FallsBackToASharedBucket_WhenNothingIdentifiesTheCaller()
    {
        var context = ContextFor(logtoUserId: null, remoteIp: null);

        RateLimitPartitioning.ResolvePartitionKey(context)
            .Should().Be(RateLimitPartitioning.UnknownPartition);
    }

    private static DefaultHttpContext ContextFor(string? logtoUserId, string? remoteIp)
    {
        var context = new DefaultHttpContext();

        if (logtoUserId is not null)
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, logtoUserId)],
                "Test");
            context.User = new ClaimsPrincipal(identity);
        }

        if (remoteIp is not null)
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        }

        return context;
    }
}
