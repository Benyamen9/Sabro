using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Infrastructure.Configurations;

internal sealed class SuggestedEditConfiguration : IEntityTypeConfiguration<SuggestedEdit>
{
    public void Configure(EntityTypeBuilder<SuggestedEdit> builder)
    {
        builder.ToTable("SuggestedEdits");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TargetType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.TargetId).IsRequired();

        // Nullable since field proposals arrived: prose targets carry a version,
        // Lexicon/Historical targets carry TargetUpdatedAt instead. Widening a NOT NULL
        // column is forward-compatible, so it deploys without an expand/contract dance.
        builder.Property(e => e.TargetVersion);
        builder.Property(e => e.TargetUpdatedAt);
        builder.Property(e => e.Field).HasMaxLength(128);
        builder.Property(e => e.ProposedContent).IsRequired();
        builder.Property(e => e.Rationale);
        builder.Property(e => e.SubmittedByLogtoUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.DecisionByLogtoUserId).HasMaxLength(256);
        builder.Property(e => e.DecisionAt);
        builder.Property(e => e.DecisionNote);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => new { e.TargetType, e.TargetId, e.Status });
        builder.HasIndex(e => e.SubmittedByLogtoUserId);
    }
}
