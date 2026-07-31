using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Historical.Domain;

namespace Sabro.Historical.Infrastructure.Configurations;

internal sealed class HistoricalFigureConfiguration : IEntityTypeConfiguration<HistoricalFigure>
{
    public void Configure(EntityTypeBuilder<HistoricalFigure> builder)
    {
        builder.ToTable("HistoricalFigures");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Era).IsRequired();

        // String-converted enums (house rule): adding values is an ordinary
        // migration; renaming existing ones is a breaking /api/v1/ change.
        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Period).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Region).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Tradition).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.Gender).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.Property(e => e.PlayableInShmo).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.Name);

        // Serves the Shmo eligible-pool query (Published + playable).
        builder.HasIndex(e => new { e.Status, e.PlayableInShmo });

        // Descriptions — owned-many on the private list, in a child table keyed by
        // (HistoricalFigureId, Language). Keyed on the language rather than on a
        // Position ordinal as the Lexicon's meanings are: order carries no meaning
        // here (the reveal card looks a description up by language), and the key
        // then enforces the one-per-language rule in the database as well as in
        // the aggregate.
        builder.OwnsMany<HistoricalFigureDescription>("descriptions", db =>
        {
            db.ToTable("HistoricalFigureDescriptions");
            db.WithOwner().HasForeignKey("HistoricalFigureId");
            db.HasKey("HistoricalFigureId", nameof(HistoricalFigureDescription.Language));
            db.Property(d => d.Language).HasMaxLength(8).IsRequired();
            db.Property(d => d.Text).HasMaxLength(HistoricalFigureDescription.MaxTextLength).IsRequired();
        });
        builder.Metadata.FindNavigation("descriptions")!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(e => e.Descriptions);
    }
}
