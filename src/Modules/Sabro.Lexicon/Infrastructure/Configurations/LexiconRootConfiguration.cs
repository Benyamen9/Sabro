using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Lexicon.Domain;

namespace Sabro.Lexicon.Infrastructure.Configurations;

internal sealed class LexiconRootConfiguration : IEntityTypeConfiguration<LexiconRoot>
{
    public void Configure(EntityTypeBuilder<LexiconRoot> builder)
    {
        builder.ToTable("LexiconRoots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Form).HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Form).IsUnique();
    }
}
