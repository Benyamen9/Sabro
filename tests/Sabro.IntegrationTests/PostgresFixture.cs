using Microsoft.EntityFrameworkCore;
using Sabro.Biblical.Infrastructure;
using Sabro.Historical.Infrastructure;
using Sabro.Identity.Domain;
using Sabro.Identity.Infrastructure;
using Sabro.Lexicon.Infrastructure;
using Sabro.Play.Infrastructure;
using Sabro.Reviews.Infrastructure;
using Sabro.Translations.Infrastructure;
using Testcontainers.PostgreSql;

namespace Sabro.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// The <c>sub</c> the test auth handler issues when a test does not override it.
    /// Kept in step with <c>TestAuthHandler</c>.
    /// </summary>
    public const string DefaultTestUser = "integration-test-user";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("sabro_test")
        .WithUsername("sabro")
        .WithPassword("sabro")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await container.StartAsync(ct);

        await using (var translations = CreateContext())
        {
            await translations.Database.MigrateAsync(ct);
        }

        await using (var lexicon = CreateLexiconContext())
        {
            await lexicon.Database.MigrateAsync(ct);
        }

        await using (var identity = CreateIdentityContext())
        {
            await identity.Database.MigrateAsync(ct);
        }

        await using (var biblical = CreateBiblicalContext())
        {
            await biblical.Database.MigrateAsync(ct);
        }

        await using (var historical = CreateHistoricalContext())
        {
            await historical.Database.MigrateAsync(ct);
        }

        await using (var play = CreatePlayContext())
        {
            await play.Database.MigrateAsync(ct);
        }

        await using var reviews = CreateReviewsContext();
        await reviews.Database.MigrateAsync(ct);

        await EnsureDefaultUserIsOwnerAsync(ct);
    }

    /// <summary>
    /// Gives the default test caller the Owner role.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The admin endpoints check a Sabro role as well as the Logto scope, and the
    /// test auth handler can only supply the scope — the role lives in the database.
    /// Without this every admin test authenticates as a role-less user and is
    /// correctly refused, which says nothing about the behaviour under test.
    /// </para>
    /// <para>
    /// Re-callable: any test that clears the profiles table must call this again so
    /// the classes running after it are not left with a role-less caller.
    /// </para>
    /// </remarks>
    public async Task EnsureDefaultUserIsOwnerAsync(CancellationToken cancellationToken)
    {
        await using var identity = CreateIdentityContext();

        var profile = await identity.UserProfiles
            .FirstOrDefaultAsync(p => p.LogtoUserId == DefaultTestUser, cancellationToken);
        if (profile is null)
        {
            profile = UserProfile.Create(DefaultTestUser).Value!;
            identity.UserProfiles.Add(profile);
        }

        profile.AssignRole(Role.Owner);
        await identity.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public TranslationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TranslationsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new TranslationsDbContext(options);
    }

    public LexiconDbContext CreateLexiconContext()
    {
        var options = new DbContextOptionsBuilder<LexiconDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new LexiconDbContext(options);
    }

    public IdentityDbContext CreateIdentityContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new IdentityDbContext(options);
    }

    public BiblicalDbContext CreateBiblicalContext()
    {
        var options = new DbContextOptionsBuilder<BiblicalDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new BiblicalDbContext(options);
    }

    public ReviewsDbContext CreateReviewsContext()
    {
        var options = new DbContextOptionsBuilder<ReviewsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ReviewsDbContext(options);
    }

    public HistoricalDbContext CreateHistoricalContext()
    {
        var options = new DbContextOptionsBuilder<HistoricalDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new HistoricalDbContext(options);
    }

    public PlayDbContext CreatePlayContext()
    {
        var options = new DbContextOptionsBuilder<PlayDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new PlayDbContext(options);
    }
}
