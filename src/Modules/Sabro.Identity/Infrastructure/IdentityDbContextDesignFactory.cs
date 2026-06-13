using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sabro.Identity.Infrastructure;

/// <summary>
/// Used by the EF Core CLI (`dotnet ef`) for design-time operations such as `migrations add`.
/// The connection string here is a placeholder — actual connections at runtime use the one
/// registered through IdentityModule.RegisterServices.
/// </summary>
public sealed class IdentityDbContextDesignFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        // Read the connection from the same env the API uses so the CD
        // migrator targets the real database; fall back to the local dev
        // string for `migrations add` / `database update` on a dev machine.
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Sabro")
            ?? "Host=localhost;Port=5433;Database=sabro_dev;Username=sabro;Password=sabro";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new IdentityDbContext(options);
    }
}
