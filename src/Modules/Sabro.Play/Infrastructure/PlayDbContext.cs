using Microsoft.EntityFrameworkCore;
using Sabro.Play.Domain;
using Sabro.Shared.Infrastructure.Persistence;

namespace Sabro.Play.Infrastructure;

public sealed class PlayDbContext : DbContext
{
    public const string SchemaName = "play";

    public PlayDbContext(DbContextOptions<PlayDbContext> options)
        : base(options)
    {
    }

    public DbSet<GameResult> GameResults => Set<GameResult>();

    public DbSet<MelthoDailyPuzzle> MelthoDailyPuzzles => Set<MelthoDailyPuzzle>();

    public DbSet<MnoDailyPuzzle> MnoDailyPuzzles => Set<MnoDailyPuzzle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlayDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
