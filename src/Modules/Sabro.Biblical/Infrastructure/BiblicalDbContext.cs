using Microsoft.EntityFrameworkCore;
using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Infrastructure;

public sealed class BiblicalDbContext : DbContext
{
    public const string SchemaName = "biblical";

    public BiblicalDbContext(DbContextOptions<BiblicalDbContext> options)
        : base(options)
    {
    }

    public DbSet<BiblicalBook> Books => Set<BiblicalBook>();

    public DbSet<BiblicalPassage> Passages => Set<BiblicalPassage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BiblicalDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
