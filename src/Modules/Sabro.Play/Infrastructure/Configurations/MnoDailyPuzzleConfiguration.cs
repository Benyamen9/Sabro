using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Play.Domain;

namespace Sabro.Play.Infrastructure.Configurations;

internal sealed class MnoDailyPuzzleConfiguration : IEntityTypeConfiguration<MnoDailyPuzzle>
{
    public void Configure(EntityTypeBuilder<MnoDailyPuzzle> builder)
    {
        builder.ToTable("MnoDailyPuzzles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.GameId).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Date).IsRequired();

        // String-converted enum (house rule): adding levels is an ordinary
        // migration; renaming existing ones would be a breaking change.
        builder.Property(e => e.Difficulty).HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.Property(e => e.Expression).HasMaxLength(64).IsRequired();
        builder.Property(e => e.TileForm).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Target).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // One puzzle per game, per day, per ladder level — the backbone of
        // get-or-create selection.
        builder.HasIndex(e => new { e.GameId, e.Date, e.Difficulty }).IsUnique();

        // Serves the recent-expression replay guard (expressions served lately for a game).
        builder.HasIndex(e => new { e.GameId, e.Expression });
    }
}
