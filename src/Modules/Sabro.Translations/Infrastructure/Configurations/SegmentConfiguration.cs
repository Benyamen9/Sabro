using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Translations.Domain;

namespace Sabro.Translations.Infrastructure.Configurations;

internal sealed class SegmentConfiguration : IEntityTypeConfiguration<Segment>
{
    public void Configure(EntityTypeBuilder<Segment> builder)
    {
        builder.ToTable("Segments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SourceId).IsRequired();
        builder.Property(e => e.ChapterNumber).IsRequired();
        builder.Property(e => e.VerseNumber).IsRequired();
        builder.Property(e => e.TextVersionId).IsRequired();
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.Version).IsRequired();
        builder.Property(e => e.PreviousVersionId);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasOne<Source>()
            .WithMany()
            .HasForeignKey(e => e.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TextVersion>()
            .WithMany()
            .HasForeignKey(e => e.TextVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Segment>()
            .WithMany()
            .HasForeignKey(e => e.PreviousVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SourceId, e.ChapterNumber, e.VerseNumber, e.TextVersionId, e.Version })
            .IsUnique();
    }
}
