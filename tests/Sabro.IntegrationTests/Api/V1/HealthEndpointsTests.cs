using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Sabro.IntegrationTests.Api.V1;

/// <summary>
/// Pins the liveness/readiness split that came out of the 2026-07-31 disk-full outage:
/// <c>/health</c> must fail when the database is unreachable (it is the URL UptimeRobot
/// watches, and it reported 200 through a total data outage), while <c>/health/live</c>
/// must stay green regardless, so it is safe for container health checks.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class HealthEndpointsTests : IDisposable
{
    /// <summary>
    /// A guaranteed-closed local port: connecting fails immediately with "connection
    /// refused" rather than burning the probe budget on a DNS or TCP timeout.
    /// </summary>
    private const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=sabro;Username=sabro;Password=sabro;Timeout=2";

    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public HealthEndpointsTests(PostgresFixture postgres)
    {
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_WithReachableDatabase_Returns200AndReportsPostgres()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/health", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(
            SabroApiFactory.JsonOptions, ct);
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().ContainKey("postgres");
        body.Checks["postgres"].Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_ProbedWithHead_Returns200()
    {
        // UptimeRobot probes with HEAD, not GET — the production monitor exercises
        // this exact verb, so it is the one worth pinning.
        var ct = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Head, "/health");

        var response = await client.SendAsync(request, ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_WithUnreachableDatabase_Returns503()
    {
        var ct = TestContext.Current.CancellationToken;
        using var brokenClient = CreateClientWithUnreachableDatabase();

        var response = await brokenClient.GetAsync("/health", ct);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(
            SabroApiFactory.JsonOptions, ct);
        body!.Status.Should().Be("Unhealthy");
        body.Checks["postgres"].Status.Should().Be("Unhealthy");
    }

    [Fact]
    public async Task Health_WhenUnhealthy_DoesNotLeakConnectionDetails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var brokenClient = CreateClientWithUnreachableDatabase();

        var response = await brokenClient.GetAsync("/health", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // The caller is an anonymous monitor: it learns *that* Postgres is down, never
        // the host, port, credentials, or the raw exception. Those go to Seq instead.
        body.Should().NotContain("127.0.0.1");
        body.Should().NotContain("Password");
        body.Should().NotContain("Npgsql");
    }

    [Fact]
    public async Task HealthLive_WithUnreachableDatabase_StaysGreen()
    {
        // The whole point of the split. If this ever starts failing on a database blip,
        // wiring it into a Docker health check restarts containers during an outage —
        // the 2026-07-28 failure mode in a new costume.
        var ct = TestContext.Current.CancellationToken;
        using var brokenClient = CreateClientWithUnreachableDatabase();

        var response = await brokenClient.GetAsync("/health/live", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(
            SabroApiFactory.JsonOptions, ct);
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().BeEmpty();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private HttpClient CreateClientWithUnreachableDatabase()
    {
        var brokenFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Sabro"] = UnreachableDatabase,
                })));

        return brokenFactory.CreateClient();
    }

    private sealed record HealthResponse(string Status, Dictionary<string, HealthCheckEntry> Checks);

    private sealed record HealthCheckEntry(string Status, string? Description);
}
