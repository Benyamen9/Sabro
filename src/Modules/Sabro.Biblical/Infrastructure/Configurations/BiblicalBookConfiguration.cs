using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Infrastructure.Configurations;

internal sealed class BiblicalBookConfiguration : IEntityTypeConfiguration<BiblicalBook>
{
    public void Configure(EntityTypeBuilder<BiblicalBook> builder)
    {
        builder.ToTable("BiblicalBooks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(8).IsRequired();
        builder.Property(e => e.EnglishName).HasMaxLength(64).IsRequired();
        builder.Property(e => e.SyriacName).HasMaxLength(64);
        builder.Property(e => e.Testament).HasConversion<string>().HasMaxLength(8).IsRequired();
        builder.Property(e => e.Order).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
    }
}
