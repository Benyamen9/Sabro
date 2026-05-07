using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sabro.IntegrationTests.Api;

public sealed class SabroApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Mirrors the JSON contract configured in <c>Program.cs</c> (camelCase + string enums)
    /// so test assertions deserialize responses the same way real clients do.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Default Meilisearch URL used when no real engine is wired in. Points at
    /// a guaranteed-closed local port so the startup search initializer fails
    /// fast (connection refused) instead of waiting on the request timeout.
    /// Controller tests don't assert on search state — that's covered by the
    /// dedicated *SearchSyncTests classes — so a no-op endpoint is fine here.
    /// </summary>
    private const string FastFailingMeilisearchUrl = "http://127.0.0.1:1";

    private readonly string connectionString;
    private readonly string meilisearchUrl;

    public SabroApiFactory(string connectionString)
        : this(connectionString, FastFailingMeilisearchUrl)
    {
    }

    public SabroApiFactory(string connectionString, string meilisearchUrl)
    {
        this.connectionString = connectionString;
        this.meilisearchUrl = meilisearchUrl;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sabro"] = connectionString,
                ["Logto:Authority"] = "https://logto.test/",
                ["Logto:Audience"] = "https://sabro.local/api",
                ["Meilisearch:Url"] = meilisearchUrl,
                ["Meilisearch:RequestTimeout"] = "00:00:02",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}
