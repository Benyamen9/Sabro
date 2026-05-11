using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Infrastructure.Configurations;

internal sealed class BiblicalPassageConfiguration : IEntityTypeConfiguration<BiblicalPassage>
{
    public void Configure(EntityTypeBuilder<BiblicalPassage> builder)
    {
        builder.ToTable("BiblicalPassages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BookId).IsRequired();
        builder.Property(e => e.ChapterNumber).IsRequired();
        builder.Property(e => e.VerseNumber).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => new { e.BookId, e.ChapterNumber, e.VerseNumber }).IsUnique();
    }
}
