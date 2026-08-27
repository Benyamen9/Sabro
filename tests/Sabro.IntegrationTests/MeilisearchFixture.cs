using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Meilisearch;

namespace Sabro.IntegrationTests;

/// <summary>
/// Spins up a single Meilisearch container shared across all tests in the
/// xUnit collection. Mirrors the way <see cref="PostgresFixture"/> shares one
/// PostgreSQL container — we trade test isolation for setup cost, and let
/// each test pick its own index name to stay independent.
/// </summary>
public sealed class MeilisearchFixture : IAsyncLifetime
{
    private const string MasterKey = "test-master-key";

    // Matches docker-compose.prod.yml. It sat on v1.13 while production moved to
    // v1.53, so index behaviour — settings, filterable attributes, synonyms — was
    // verified against an engine forty minor versions behind the one that serves
    // it. Keep this in step whenever the compose pin moves.
    //
    // Each run gets a fresh container and no volume, so this never meets the
    // data-directory incompatibility that governs a production upgrade.
    private readonly IContainer container = new ContainerBuilder("getmeili/meilisearch:v1.53")
        .WithPortBinding(7700, true)
        .WithEnvironment("MEILI_MASTER_KEY", MasterKey)
        .WithEnvironment("MEILI_NO_ANALYTICS", "true")
        .WithEnvironment("MEILI_ENV", "development")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r =>
            r.ForPath("/health").ForPort(7700)))
        .Build();

    public string Url => $"http://{container.Hostname}:{container.GetMappedPublicPort(7700)}";

    public MeilisearchClient CreateClient() => new(Url, MasterKey);

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await container.StartAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
