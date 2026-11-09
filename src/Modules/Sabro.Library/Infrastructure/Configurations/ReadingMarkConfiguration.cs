using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Library.Domain;

namespace Sabro.Library.Infrastructure.Configurations;

internal sealed class ReadingMarkConfiguration : IEntityTypeConfiguration<ReadingMark>
{
    public void Configure(EntityTypeBuilder<ReadingMark> builder)
    {
        builder.ToTable("ReadingMarks");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LogtoUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Book).HasMaxLength(32).IsRequired();
        builder.Property(e => e.ItemKey).HasMaxLength(64).IsRequired();
        builder.Property(e => e.MarkedAt).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // One mark per reader, per item. Its leading column also serves "everything of mine".
        builder.HasIndex(e => new { e.LogtoUserId, e.Book, e.ItemKey }).IsUnique();
    }
}
