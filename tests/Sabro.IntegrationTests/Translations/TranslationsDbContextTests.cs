using Microsoft.EntityFrameworkCore;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;
using Testcontainers.PostgreSql;

namespace Sabro.IntegrationTests.Translations;

public class TranslationsDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("sabro_test")
        .WithUsername("sabro")
        .WithPassword("sabro")
        .Build();

    private string ConnectionString => container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await container.StartAsync(ct);
        await using var context = CreateContext();
        await context.Database.MigrateAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Author_RoundTrip_PreservesAllFields()
    {
        var ct = TestContext.Current.CancellationToken;

        var author = Author.Create(
            name: "Dionysios bar Salibi",
            syriacName: "ܕܝܘܢܘܣܝܘܣ",
            title: "Metropolitan of Amid").Value!;

        await using (var writeContext = CreateContext())
        {
            writeContext.Authors.Add(author);
            await writeContext.SaveChangesAsync(ct);
        }

        await using var readContext = CreateContext();
        var loaded = await readContext.Authors.FirstOrDefaultAsync(a => a.Id == author.Id, ct);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(author.Id);
        loaded.Name.Should().Be(author.Name);
        loaded.SyriacName.Should().Be(author.SyriacName);
        loaded.Title.Should().Be(author.Title);
        loaded.CreatedAt.Should().BeCloseTo(author.CreatedAt, TimeSpan.FromMilliseconds(1));
        loaded.UpdatedAt.Should().BeCloseTo(author.UpdatedAt, TimeSpan.FromMilliseconds(1));
    }

    private TranslationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TranslationsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new TranslationsDbContext(options);
    }
}
