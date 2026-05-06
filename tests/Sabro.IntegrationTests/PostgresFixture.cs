using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Infrastructure;
using Sabro.Translations.Infrastructure;
using Testcontainers.PostgreSql;

namespace Sabro.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
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

        await using var lexicon = CreateLexiconContext();
        await lexicon.Database.MigrateAsync(ct);
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
}
