using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Domain;

namespace Sabro.Lexicon.Infrastructure;

public sealed class LexiconDbContext : DbContext
{
    public const string SchemaName = "lexicon";

    public LexiconDbContext(DbContextOptions<LexiconDbContext> options)
        : base(options)
    {
    }

    public DbSet<LexiconRoot> Roots => Set<LexiconRoot>();

    public DbSet<LexiconEntry> Entries => Set<LexiconEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LexiconDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
