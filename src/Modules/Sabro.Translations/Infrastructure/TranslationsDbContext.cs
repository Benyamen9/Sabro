using Microsoft.EntityFrameworkCore;
using Sabro.Translations.Domain;

namespace Sabro.Translations.Infrastructure;

public sealed class TranslationsDbContext : DbContext
{
    public const string SchemaName = "translations";

    public TranslationsDbContext(DbContextOptions<TranslationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<TextVersion> TextVersions => Set<TextVersion>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Source> Sources => Set<Source>();

    public DbSet<Segment> Segments => Set<Segment>();

    public DbSet<Annotation> Annotations => Set<Annotation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TranslationsDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
