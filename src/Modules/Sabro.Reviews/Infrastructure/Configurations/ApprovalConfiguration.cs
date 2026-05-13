using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Infrastructure.Configurations;

internal sealed class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("Approvals");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TargetType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(a => a.SourceId).IsRequired();
        builder.Property(a => a.ChapterNumber).IsRequired();
        builder.Property(a => a.VerseNumber);
        builder.Property(a => a.Version);
        builder.Property(a => a.AnnotationId);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(a => a.DecisionByLogtoUserId).HasMaxLength(256).IsRequired();
        builder.Property(a => a.DecisionAt).IsRequired();
        builder.Property(a => a.Note);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => new { a.SourceId, a.ChapterNumber, a.VerseNumber });
        builder.HasIndex(a => new { a.SourceId, a.ChapterNumber, a.TargetType });
        builder.HasIndex(a => a.AnnotationId);
        builder.HasIndex(a => a.DecisionByLogtoUserId);
    }
}
