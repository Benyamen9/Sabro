using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sabro.Identity.Domain;

namespace Sabro.Identity.Infrastructure.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LogtoUserId).HasMaxLength(256).IsRequired();
        builder.Property(e => e.PreferredLanguage).HasMaxLength(8).IsRequired();
        builder.Property(e => e.PreferredScriptVariant).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(UserProfile.MaxDisplayNameLength);
        builder.Property(e => e.ShowOnLeaderboard).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => e.LogtoUserId).IsUnique();
    }
}
