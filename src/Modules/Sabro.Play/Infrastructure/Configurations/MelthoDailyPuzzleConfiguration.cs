using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Play.Domain;

namespace Sabro.Play.Infrastructure.Configurations;

internal sealed class MelthoDailyPuzzleConfiguration : IEntityTypeConfiguration<MelthoDailyPuzzle>
{
    public void Configure(EntityTypeBuilder<MelthoDailyPuzzle> builder)
    {
        builder.ToTable("MelthoDailyPuzzles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.GameId).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Date).IsRequired();
        builder.Property(e => e.LexiconEntryId).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // One puzzle per game, per day — the backbone of get-or-create selection.
        builder.HasIndex(e => new { e.GameId, e.Date }).IsUnique();

        // Serves the anti-repetition window scan (recently served entries for a game).
        builder.HasIndex(e => new { e.GameId, e.LexiconEntryId });
    }
}
