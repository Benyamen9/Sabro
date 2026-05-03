using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sabro.Translations.Infrastructure;

/// <summary>
/// Used by the EF Core CLI (`dotnet ef`) for design-time operations such as `migrations add`.
/// The connection string here is a placeholder — actual connections at runtime use the one
/// registered through TranslationsModule.RegisterServices.
/// </summary>
public sealed class TranslationsDbContextDesignFactory : IDesignTimeDbContextFactory<TranslationsDbContext>
{
    public TranslationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TranslationsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sabro_dev;Username=sabro;Password=sabro")
            .Options;

        return new TranslationsDbContext(options);
    }
}
