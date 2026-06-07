using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Play.Domain;

namespace Sabro.Play.Infrastructure.Configurations;

internal sealed class GameResultConfiguration : IEntityTypeConfiguration<GameResult>
{
    public void Configure(EntityTypeBuilder<GameResult> builder)
    {
        builder.ToTable("GameResults");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LogtoUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.GameId).HasMaxLength(32).IsRequired();
        builder.Property(e => e.PlayedOn).IsRequired();
        builder.Property(e => e.Solved).IsRequired();
        builder.Property(e => e.Attempts).IsRequired();
        builder.Property(e => e.DetailJson).HasColumnType("jsonb");
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        // One result per user, per game, per day. Its leading columns also serve
        // the "my results" listing (filter by user, order by played-on).
        builder.HasIndex(e => new { e.LogtoUserId, e.GameId, e.PlayedOn }).IsUnique();
    }
}
