using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Infrastructure.Configurations;

internal sealed class CrossReferenceConfiguration : IEntityTypeConfiguration<CrossReference>
{
    public void Configure(EntityTypeBuilder<CrossReference> builder)
    {
        builder.ToTable("CrossReferences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AnnotationAnchorId).IsRequired();
        builder.Property(e => e.PassageId).IsRequired();

        // Persisted as the enum member name so adding values is a plain code change
        // + ordinary migration, never raw ALTER TYPE.
        builder.Property(e => e.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.AnnotationAnchorId);
        builder.HasIndex(e => e.PassageId);
        builder.HasIndex(e => new { e.AnnotationAnchorId, e.PassageId, e.Source, e.Kind }).IsUnique();
    }
}
