using Microsoft.EntityFrameworkCore;
using Sabro.Library.Domain;
using Sabro.Shared.Infrastructure.Persistence;

namespace Sabro.Library.Infrastructure;

public sealed class LibraryDbContext : DbContext
{
    public const string SchemaName = "library";

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReadingPlace> ReadingPlaces => Set<ReadingPlace>();

    public DbSet<ReadingMark> ReadingMarks => Set<ReadingMark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
