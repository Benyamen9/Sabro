using Sabro.Identity.Domain;

namespace Sabro.Identity.Application.UserProfiles;

public sealed record UpdateUserProfileRequest(
    string PreferredLanguage,
    ScriptVariant PreferredScriptVariant);
