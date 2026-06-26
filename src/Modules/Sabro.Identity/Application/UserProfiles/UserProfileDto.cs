using Sabro.Identity.Domain;

namespace Sabro.Identity.Application.UserProfiles;

public sealed record UserProfileDto(
    Guid Id,
    string LogtoUserId,
    string PreferredLanguage,
    ScriptVariant PreferredScriptVariant,
    Role Role,
    string? DisplayName,
    bool ShowOnLeaderboard,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
