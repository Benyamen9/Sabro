using Microsoft.EntityFrameworkCore;
using Sabro.Identity.Domain;

namespace Sabro.Identity.Infrastructure;

public sealed class IdentityDbContext : DbContext
{
    public const string SchemaName = "identity";

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.UseSnakeCaseNaming();
    }
}
