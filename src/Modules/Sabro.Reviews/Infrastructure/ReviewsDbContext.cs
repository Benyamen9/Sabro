using Microsoft.EntityFrameworkCore;
using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Infrastructure;

public sealed class ReviewsDbContext : DbContext
{
    public const string SchemaName = "reviews";

    public ReviewsDbContext(DbContextOptions<ReviewsDbContext> options)
        : base(options)
    {
    }

    public DbSet<SuggestedEdit> SuggestedEdits => Set<SuggestedEdit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
