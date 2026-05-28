using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sabro.Reviews.Infrastructure;

/// <summary>
/// Used by the EF Core CLI (`dotnet ef`) for design-time operations such as `migrations add`.
/// The connection string here is a placeholder — actual connections at runtime use the one
/// registered through ReviewsModule.RegisterServices.
/// </summary>
public sealed class ReviewsDbContextDesignFactory : IDesignTimeDbContextFactory<ReviewsDbContext>
{
    public ReviewsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReviewsDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=sabro_dev;Username=sabro;Password=sabro")
            .Options;

        return new ReviewsDbContext(options);
    }
}
