using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Infrastructure.Configurations;

internal sealed class ChantConfiguration : IEntityTypeConfiguration<Chant>
{
    public void Configure(EntityTypeBuilder<Chant> builder)
    {
        builder.ToTable("Chants");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SyriacIncipit).HasMaxLength(512).IsRequired();
        builder.Property(e => e.SyriacIncipitVocalized).HasMaxLength(512);
        builder.Property(e => e.Transliteration).HasMaxLength(Chant.MaxTransliterationLength).IsRequired();
        builder.Property(e => e.Shuhlofo).HasMaxLength(Chant.MaxShuhlofoLength);
        builder.Property(e => e.AudioUrl).HasMaxLength(Chant.MaxAudioUrlLength);

        // String-converted enum (house rule): adding values is an ordinary
        // migration; renaming existing ones is a breaking /api/v1/ change.
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.Property(e => e.ModeId).IsRequired();
        builder.Property(e => e.PlayableInNahlo).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasOne<BethGazoMode>()
            .WithMany()
            .HasForeignKey(e => e.ModeId)
            .OnDelete(DeleteBehavior.Restrict);

        // A solqin's parent. Restrict, not cascade: deleting a melody must not
        // silently take its solqin with it.
        builder.HasOne<Chant>()
            .WithMany()
            .HasForeignKey(e => e.InheritsMelodyFromId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Transliteration);

        // Serves the Nahlo eligible-pool query (Published + playable).
        builder.HasIndex(e => new { e.Status, e.PlayableInNahlo });

        // Identity: a melody name recurs across modes, and within a mode it may
        // recur across shuḥlofe — so the chant is the triple, and only the triple.
        //
        // Two indexes rather than one, because Postgres treats NULLs as distinct
        // in a unique index: a single index over the three columns would happily
        // accept "Maryam yoldath Aloho / Tlithoyo / NULL" twice, which is exactly
        // the duplicate this is meant to stop. The filtered pair covers both
        // cases without resorting to a sentinel string standing in for "no
        // variation" — a sentinel would then have to be stripped everywhere the
        // value is read.
        builder.HasIndex(e => new { e.Transliteration, e.ModeId, e.Shuhlofo })
            .IsUnique()
            .HasFilter("shuhlofo IS NOT NULL")
            .HasDatabaseName("ix_chants_identity_with_shuhlofo");

        builder.HasIndex(e => new { e.Transliteration, e.ModeId })
            .IsUnique()
            .HasFilter("shuhlofo IS NULL")
            .HasDatabaseName("ix_chants_identity_without_shuhlofo");
    }
}
