using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sabro.Identity.Domain;
using Sabro.Identity.Infrastructure;

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

        EnsureDefaultUserIsOwner(connectionString);
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

    /// <summary>
    /// Guarantees the default test caller holds <see cref="Role.Owner"/> before any
    /// request is made through this factory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The admin endpoints check a Sabro role as well as the Logto scope, and
    /// <c>TestAuthHandler</c> can only supply the scope — the role lives in the
    /// database. Seeding it once per fixture is not enough: several suites mutate
    /// the shared profiles table (the leaderboard tests clear it outright, the
    /// People tests clear it to exercise the no-Owner bootstrap, and the reviewer
    /// suites reassign roles), so whether an admin test finds an Owner depended on
    /// which class happened to run first. That difference is invisible locally and
    /// showed up only as a CI failure.
    /// </para>
    /// <para>
    /// Doing it per factory — one per test class instance, constructed before each
    /// test — removes the ordering dependency entirely. Tests that deliberately want
    /// a different role state still set it afterwards, which runs later and wins.
    /// </para>
    /// </remarks>
    private static void EnsureDefaultUserIsOwner(string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var identity = new IdentityDbContext(options);

        var profile = identity.UserProfiles
            .FirstOrDefault(p => p.LogtoUserId == PostgresFixture.DefaultTestUser);
        if (profile is null)
        {
            profile = UserProfile.Create(PostgresFixture.DefaultTestUser).Value!;
            identity.UserProfiles.Add(profile);
        }

        profile.AssignRole(Role.Owner);
        identity.SaveChanges();
    }
}
