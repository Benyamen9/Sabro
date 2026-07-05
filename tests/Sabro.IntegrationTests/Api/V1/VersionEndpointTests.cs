using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Sabro.IntegrationTests.Api.V1;

[Collection(IntegrationCollection.Name)]
public class VersionEndpointTests : IDisposable
{
    private readonly SabroApiFactory factory;
    private readonly HttpClient client;

    public VersionEndpointTests(PostgresFixture postgres)
    {
        factory = new SabroApiFactory(postgres.ConnectionString);
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_WithoutConfiguredBuildSha_Returns200WithUnknown()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await client.GetAsync("/version", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VersionResponse>(ct);
        body!.Sha.Should().Be("unknown");
    }

    [Fact]
    public async Task Get_WithConfiguredBuildSha_ReturnsThatSha()
    {
        var ct = TestContext.Current.CancellationToken;
        using var shaFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["BUILD_SHA"] = "abc123def456",
                })));
        using var shaClient = shaFactory.CreateClient();

        var response = await shaClient.GetAsync("/version", ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VersionResponse>(ct);
        body!.Sha.Should().Be("abc123def456");
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed record VersionResponse(string Sha);
}
