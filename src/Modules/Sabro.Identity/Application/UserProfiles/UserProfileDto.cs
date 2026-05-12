using Sabro.Identity.Domain;

namespace Sabro.Identity.Application.UserProfiles;

public sealed record UserProfileDto(
    Guid Id,
    string LogtoUserId,
    string PreferredLanguage,
    ScriptVariant PreferredScriptVariant,
    Role Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
