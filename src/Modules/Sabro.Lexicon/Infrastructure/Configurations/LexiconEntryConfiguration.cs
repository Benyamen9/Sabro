using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Lexicon.Domain;

namespace Sabro.Lexicon.Infrastructure.Configurations;

internal sealed class LexiconEntryConfiguration : IEntityTypeConfiguration<LexiconEntry>
{
    public void Configure(EntityTypeBuilder<LexiconEntry> builder)
    {
        builder.ToTable("LexiconEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SyriacUnvocalized).HasMaxLength(256).IsRequired();
        builder.Property(e => e.SyriacVocalized).HasMaxLength(256);
        builder.Property(e => e.RootId);
        builder.Property(e => e.SblTransliteration).HasMaxLength(128).IsRequired();
        builder.Property(e => e.GrammaticalCategory)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(e => e.Morphology);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // TransliterationVariants — backed by the private List<string> field of the same name.
        // Npgsql maps List<string> to PostgreSQL text[] natively.
        builder.Ignore(e => e.TransliterationVariants);
        builder.Property<List<string>>("transliterationVariants")
            .HasColumnName("transliteration_variants")
            .HasColumnType("text[]")
            .IsRequired();

        // FK to LexiconRoot — nullable, set to null on root deletion.
        builder.HasOne<LexiconRoot>()
            .WithMany()
            .HasForeignKey(e => e.RootId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(e => e.RootId);

        builder.HasIndex(e => e.SyriacUnvocalized);
        builder.HasIndex(e => e.SblTransliteration);

        // Meanings — owned-many on a private List<LexiconMeaning> field, persisted to a child table
        // with composite key (LexiconEntryId, Position). EF preserves order by writing Position.
        builder.OwnsMany<LexiconMeaning>("meanings", mb =>
        {
            mb.ToTable("LexiconEntryMeanings");
            mb.WithOwner().HasForeignKey("LexiconEntryId");
            mb.Property<int>("Position");
            mb.HasKey("LexiconEntryId", "Position");
            mb.Property(m => m.Language).HasMaxLength(8).IsRequired();
            mb.Property(m => m.Text).IsRequired();
        });
        builder.Metadata.FindNavigation("meanings")!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(e => e.Meanings);
    }
}
