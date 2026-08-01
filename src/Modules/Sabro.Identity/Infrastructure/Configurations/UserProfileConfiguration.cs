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

        // Owned child collection keyed on (profile, area): the database enforces one
        // grant per area per person, so "reviewer AND editor for the Lexicon" cannot
        // exist even if a caller asks for it. Absence of a row is the only way to say
        // "no access" — there is deliberately no None value to disagree with it.
        builder.OwnsMany(e => e.AreaPermissions, area =>
        {
            area.ToTable("UserAreaPermissions");
            area.WithOwner().HasForeignKey("UserProfileId");
            area.Property(a => a.Area).HasConversion<string>().HasMaxLength(32).IsRequired();
            area.Property(a => a.Access).HasConversion<string>().HasMaxLength(32).IsRequired();
            area.HasKey("UserProfileId", nameof(AreaGrant.Area));
        });
    }
}
