using Microsoft.EntityFrameworkCore;
using Sabro.BethGazo.Domain;
using Sabro.Shared.Infrastructure.Persistence;

namespace Sabro.BethGazo.Infrastructure;

public sealed class BethGazoDbContext : DbContext
{
    public const string SchemaName = "beth_gazo";

    public BethGazoDbContext(DbContextOptions<BethGazoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chant> Chants => Set<Chant>();

    public DbSet<BethGazoMode> Modes => Set<BethGazoMode>();

    public DbSet<BethGazoSection> Sections => Set<BethGazoSection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BethGazoDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
