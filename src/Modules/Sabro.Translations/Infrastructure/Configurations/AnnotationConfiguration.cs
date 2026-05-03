using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Translations.Domain;

namespace Sabro.Translations.Infrastructure.Configurations;

internal sealed class AnnotationConfiguration : IEntityTypeConfiguration<Annotation>
{
    public void Configure(EntityTypeBuilder<Annotation> builder)
    {
        builder.ToTable("Annotations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SegmentId).IsRequired();
        builder.Property(e => e.AnchorStart).IsRequired();
        builder.Property(e => e.AnchorEnd).IsRequired();
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasOne<Segment>()
            .WithMany()
            .HasForeignKey(e => e.SegmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.SegmentId);
    }
}
