using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Library.Domain;

namespace Sabro.Library.Infrastructure.Configurations;

internal sealed class ReadingPlaceConfiguration : IEntityTypeConfiguration<ReadingPlace>
{
    public void Configure(EntityTypeBuilder<ReadingPlace> builder)
    {
        builder.ToTable("ReadingPlaces");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LogtoUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Book).HasMaxLength(32).IsRequired();
        builder.Property(e => e.ItemKey).HasMaxLength(64).IsRequired();
        builder.Property(e => e.OpenedAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // One ribbon per reader, per book. Its leading column also serves "everything of mine".
        builder.HasIndex(e => new { e.LogtoUserId, e.Book }).IsUnique();
    }
}
