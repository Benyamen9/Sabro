using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Sabro.IntegrationTests.Api;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// The rate limiter over a real pipeline: that it still refuses a flood, and that
/// it refuses it <i>per caller</i> rather than for everyone at once.
/// </summary>
/// <remarks>
/// <para>
/// The unit tests beside this one (<c>Sabro.UnitTests.Api.RateLimitPartitioningTests</c>)
/// pin the partition key in isolation. They cannot see the bug that actually
/// shipped, which was one of middleware <i>order</i>: <c>UseRateLimiter</c> ran
/// before <c>UseAuthentication</c>, so <c>User</c> was still anonymous when the key
/// was computed and every signed-in caller fell through to a single shared bucket.
/// A correct key in the wrong place is still one global window, and only a test
/// that runs the real pipeline can tell the difference.
/// </para>
/// <para>
/// The limit is lowered to a handful of requests here rather than firing the
/// production default: it keeps the test quick, and it means the test asserts the
/// partitioning behaviour rather than the current value of a tunable.
/// </para>
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class RateLimitPartitioningTests : IDisposable
{
    private const int PermitLimit = 3;

    /// <summary>
    /// Cheapest endpoint in the app: no database, no authorization, and still
    /// behind the limiter, which sits ahead of endpoint routing.
    /// </summary>
    private const string Endpoint = "/version";

    private readonly SabroApiFactory factory;
    private readonly WebApplicationFactory<Program> limited;

    public RateLimitPartitioningTests(PostgresFixture postgres)
    {
        factory = new SabroApiFactory(postgres.ConnectionString);
        limited = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimit:PermitLimit"] = PermitLimit.ToString(),
                    ["RateLimit:WindowSeconds"] = "60",
                })));
    }

    public void Dispose()
    {
        limited.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task TheLimiterStillRefusesAFloodFromOneCaller()
    {
        using var client = limited.CreateClient();

        for (var i = 0; i < PermitLimit; i++)
        {
            var allowed = await GetAsAsync(client, "flood-user");
            allowed.StatusCode.Should().Be(HttpStatusCode.OK, "request {0} is inside the window", i + 1);
        }

        var refused = await GetAsAsync(client, "flood-user");
        refused.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// The regression. One caller exhausting their window must not spend anybody
    /// else's — which is only true if authentication has run by the time the
    /// partition key is chosen.
    /// </summary>
    [Fact]
    public async Task OneCallerExhaustingTheWindowDoesNotRefuseAnother()
    {
        using var client = limited.CreateClient();

        for (var i = 0; i < PermitLimit + 1; i++)
        {
            await GetAsAsync(client, "noisy-user");
        }

        (await GetAsAsync(client, "noisy-user")).StatusCode
            .Should().Be(HttpStatusCode.TooManyRequests, "the noisy caller is over their limit");

        (await GetAsAsync(client, "quiet-user")).StatusCode
            .Should().Be(HttpStatusCode.OK, "a different caller has their own window");
    }

    private static async Task<HttpResponseMessage> GetAsAsync(HttpClient client, string user)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint);
        request.Headers.Add(TestAuthHandler.UserHeaderName, user);
        return await client.SendAsync(request);
    }
}
