using Microsoft.EntityFrameworkCore;
using Sabro.Historical.Domain;
using Sabro.Shared.Infrastructure.Persistence;

namespace Sabro.Historical.Infrastructure;

public sealed class HistoricalDbContext : DbContext
{
    public const string SchemaName = "historical";

    public HistoricalDbContext(DbContextOptions<HistoricalDbContext> options)
        : base(options)
    {
    }

    public DbSet<HistoricalFigure> Figures => Set<HistoricalFigure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HistoricalDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
