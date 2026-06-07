using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sabro.Play.Infrastructure;

/// <summary>
/// Used by the EF Core CLI (`dotnet ef`) for design-time operations such as `migrations add`.
/// The connection string here is a placeholder — actual connections at runtime use the one
/// registered through PlayModule.RegisterServices.
/// </summary>
public sealed class PlayDbContextDesignFactory : IDesignTimeDbContextFactory<PlayDbContext>
{
    public PlayDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlayDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=sabro_dev;Username=sabro;Password=sabro")
            .Options;

        return new PlayDbContext(options);
    }
}
