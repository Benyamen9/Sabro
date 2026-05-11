using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Identity.Domain;

public sealed class UserProfile : Entity<Guid>, IAggregateRoot
{
    private UserProfile(string logtoUserId, string preferredLanguage, ScriptVariant preferredScriptVariant)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        LogtoUserId = logtoUserId;
        PreferredLanguage = preferredLanguage;
        PreferredScriptVariant = preferredScriptVariant;
    }

    /// <summary>
    /// Opaque user identifier issued by Logto (the OIDC <c>sub</c> claim).
    /// Single source of truth — names and emails are not mirrored locally.
    /// </summary>
    public string LogtoUserId { get; private set; }

    public string PreferredLanguage { get; private set; }

    public ScriptVariant PreferredScriptVariant { get; private set; }

    public static Result<UserProfile> Create(
        string logtoUserId,
        string preferredLanguage = "en",
        ScriptVariant preferredScriptVariant = ScriptVariant.Estrangela)
    {
        var trimmedLogtoUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedLogtoUserId.Length == 0)
        {
            return Result<UserProfile>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var languageResult = NormalizeLanguage(preferredLanguage);
        if (!languageResult.IsSuccess)
        {
            return Result<UserProfile>.Failure(languageResult.Error!);
        }

        return Result<UserProfile>.Success(
            new UserProfile(trimmedLogtoUserId, languageResult.Value!, preferredScriptVariant));
    }

    /// <summary>
    /// Applies new preferences. Returns <c>null</c> on success, an
    /// <see cref="Error"/> on validation failure — mirroring the
    /// <c>PageRequest.Validate</c> shape so callers don't need to lift an
    /// empty <c>Result&lt;Unit&gt;</c>-style wrapper.
    /// </summary>
    public Error? UpdatePreferences(string preferredLanguage, ScriptVariant preferredScriptVariant)
    {
        var languageResult = NormalizeLanguage(preferredLanguage);
        if (!languageResult.IsSuccess)
        {
            return languageResult.Error!;
        }

        PreferredLanguage = languageResult.Value!;
        PreferredScriptVariant = preferredScriptVariant;
        UpdatedAt = DateTimeOffset.UtcNow;
        return null;
    }

    /// <summary>
    /// Trim, lower-case, and accept only the three locales the platform ships
    /// at MVP. Stored as plain ISO 639-1 so adding a fourth language later is
    /// content-only — no schema migration.
    /// </summary>
    private static Result<string> NormalizeLanguage(string language)
    {
        var trimmed = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmed.Length == 0)
        {
            return Result<string>.Failure(Error.Validation("PreferredLanguage is required."));
        }

        if (trimmed is not ("en" or "fr" or "nl"))
        {
            return Result<string>.Failure(Error.Validation("PreferredLanguage must be one of: en, fr, nl."));
        }

        return Result<string>.Success(trimmed);
    }
}
