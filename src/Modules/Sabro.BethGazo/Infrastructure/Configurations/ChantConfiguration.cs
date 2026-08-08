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

        // Whether this is a shuḥlofo, a ḥrino, or the chant in its own right.
        // String-converted like every other enum here: adding a value is an
        // ordinary migration, renaming one breaks /api/v1/.
        builder.Property(e => e.VariantKind).HasConversion<string>().HasMaxLength(16).IsRequired();

        // Which one it is (1, 2, 3 …); null exactly when the kind is None.
        // Deliberately unbounded — see Chant.VariantNumber.
        builder.Property(e => e.VariantNumber);
        builder.Property(e => e.AudioUrl).HasMaxLength(Chant.MaxAudioUrlLength);

        // String-converted enum (house rule): adding values is an ordinary
        // migration; renaming existing ones is a breaking /api/v1/ change.
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.Property(e => e.SectionId).IsRequired();

        // Nullable on purpose: null means "this section has no modes" (the
        // madroshe), never "not filled in yet". Chant.Normalize refuses either
        // reading of the column from being ambiguous.
        builder.Property(e => e.ModeId);

        builder.Property(e => e.PlayableInNahlo).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasOne<BethGazoSection>()
            .WithMany()
            .HasForeignKey(e => e.SectionId)
            .OnDelete(DeleteBehavior.Restrict);

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

        // Serves the section pickers and the "may this section drop its modes"
        // check in ChantService.
        builder.HasIndex(e => e.SectionId);

        // Identity: a melody name recurs across sections and across modes, and
        // within a mode it may recur across shuḥlofe — so the chant is the
        // quadruple, and only the quadruple.
        //
        // ONE index, not the filtered pair this used to be. Postgres treats NULLs
        // as distinct in a unique index by default, so a plain index here would
        // happily accept "Maryam yoldath Aloho / Madroshe / NULL / NULL" twice —
        // exactly the duplicate this exists to stop. That used to need two
        // filtered indexes covering shuhlofo IS NULL and IS NOT NULL; with a
        // second nullable column (mode) that approach needs FOUR, one per
        // combination, and a fifth the day a third nullable joins the identity.
        //
        // NULLS NOT DISTINCT (Postgres 15+, and we run 17) states the intent
        // directly: two rows that are null in the same places are the same row.
        // It is also why no sentinel value stands in for "no mode" — a sentinel
        // would have to be stripped again everywhere the column is read.
        builder.HasIndex(e => new { e.Transliteration, e.SectionId, e.ModeId, e.VariantKind, e.VariantNumber })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasDatabaseName("ix_chants_identity");
    }
}
