using Sabro.Identity.Domain;

namespace Sabro.Identity.Application.UserProfiles;

public sealed record UserProfileDto(
    Guid Id,
    string LogtoUserId,
    string PreferredLanguage,
    ScriptVariant PreferredScriptVariant,
    Role Role,
    IReadOnlyList<AreaGrantDto> Areas,
    string? DisplayName,
    bool ShowOnLeaderboard,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt) : IAccessProfile
{
    /// <inheritdoc />
    public AreaAccess? AccessFor(ContentArea area) =>
        Areas.FirstOrDefault(a => a.Area == area)?.Access;
}
