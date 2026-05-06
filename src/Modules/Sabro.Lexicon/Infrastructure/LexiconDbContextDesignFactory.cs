using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sabro.Lexicon.Infrastructure;

/// <summary>
/// Used by the EF Core CLI (`dotnet ef`) for design-time operations such as `migrations add`.
/// The connection string here is a placeholder — actual connections at runtime use the one
/// registered through LexiconModule.RegisterServices.
/// </summary>
public sealed class LexiconDbContextDesignFactory : IDesignTimeDbContextFactory<LexiconDbContext>
{
    public LexiconDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LexiconDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sabro_dev;Username=sabro;Password=sabro")
            .Options;

        return new LexiconDbContext(options);
    }
}
