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
        // Read the connection from the same env the API uses so the CD
        // migrator targets the real database; fall back to the local dev
        // string for `migrations add` / `database update` on a dev machine.
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Sabro")
            ?? "Host=localhost;Port=5433;Database=sabro_dev;Username=sabro;Password=sabro";

        var options = new DbContextOptionsBuilder<TranslationsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TranslationsDbContext(options);
    }
}
