using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Translations.Domain;

namespace Sabro.Translations.Infrastructure.Configurations;

internal sealed class TextVersionConfiguration : IEntityTypeConfiguration<TextVersion>
{
    public void Configure(EntityTypeBuilder<TextVersion> builder)
    {
        builder.ToTable("TextVersions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(64).IsRequired();
        builder.Property(e => e.IsRightToLeft).IsRequired();
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
    }
}
