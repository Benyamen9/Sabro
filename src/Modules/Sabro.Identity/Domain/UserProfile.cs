using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Identity.Domain;

public sealed class UserProfile : Entity<Guid>, IAggregateRoot, IAccessProfile
{
    /// <summary>Maximum length of a public display name (leaderboard, future social surfaces).</summary>
    public const int MaxDisplayNameLength = 40;

    private readonly List<AreaGrant> areaPermissions = new();

    private UserProfile(string logtoUserId, string preferredLanguage, ScriptVariant preferredScriptVariant)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        LogtoUserId = logtoUserId;
        PreferredLanguage = preferredLanguage;
        PreferredScriptVariant = preferredScriptVariant;
        Role = Role.Reader;
        ShowOnLeaderboard = false;
    }

    /// <summary>
    /// Opaque user identifier issued by Logto (the OIDC <c>sub</c> claim).
    /// Single source of truth — names and emails are not mirrored locally.
    /// </summary>
    public string LogtoUserId { get; private set; }

    public string PreferredLanguage { get; private set; }

    public ScriptVariant PreferredScriptVariant { get; private set; }

    /// <summary>
    /// Optional public-facing name, shown on the leaderboard and any future social
    /// surface. Null until the user sets one. Distinct from the Logto name (which
    /// Sabro never mirrors) so the user controls exactly how they appear.
    /// </summary>
    public string? DisplayName { get; private set; }

    /// <summary>
    /// Whether the user has opted in to appear on the public leaderboard. Defaults
    /// to <c>false</c> — appearing is a deliberate choice (the platform is private
    /// by default). Requires a <see cref="DisplayName"/> to be set.
    /// </summary>
    public bool ShowOnLeaderboard { get; private set; }

    /// <summary>
    /// Authorization role. New profiles start as <see cref="Domain.Role.Reader"/>;
    /// changes go through <see cref="AssignRole"/>. There is no public endpoint
    /// to set this at MVP — it is mutated server-side (seeding, future admin
    /// console) so the surface stays minimal until an admin UI exists.
    /// </summary>
    public Role Role { get; private set; }

    /// <summary>
    /// Per-area access. Absence of an entry means no access to that area.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="Role"/> because area and level are two questions,
    /// not one. <see cref="Role"/> now answers only "Reader, translations reviewer,
    /// or Owner"; which rooms somebody may enter, and how far, is answered here —
    /// so a person can review Shmo and edit the Lexicon at the same time.
    /// </remarks>
    public IReadOnlyList<AreaGrant> AreaPermissions => areaPermissions;

    public static Result<UserProfile> Create(
        string logtoUserId,
        string preferredLanguage = "en",
        ScriptVariant preferredScriptVariant = ScriptVariant.Serto)
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
    /// Sets the public display name and leaderboard opt-in. Returns <c>null</c> on
    /// success, an <see cref="Error"/> on validation failure (mirrors
    /// <see cref="UpdatePreferences"/>). An empty/whitespace name is stored as
    /// <c>null</c>. Opting in to the leaderboard requires a non-empty name — you
    /// cannot appear without a label.
    /// </summary>
    public Error? UpdateAccount(string? displayName, bool showOnLeaderboard)
    {
        var trimmed = displayName?.Trim();
        var normalized = string.IsNullOrEmpty(trimmed) ? null : trimmed;

        if (normalized is not null && normalized.Length > MaxDisplayNameLength)
        {
            return Error.Validation($"DisplayName must not exceed {MaxDisplayNameLength} characters.");
        }

        if (showOnLeaderboard && normalized is null)
        {
            return Error.Validation("A display name is required to appear on the leaderboard.");
        }

        DisplayName = normalized;
        ShowOnLeaderboard = showOnLeaderboard;
        UpdatedAt = DateTimeOffset.UtcNow;
        return null;
    }

    /// <summary>
    /// Assigns a new role. Returns <c>null</c> on success, an
    /// <see cref="Error"/> when the supplied value is not a defined
    /// <see cref="Role"/>. Mirrors <see cref="UpdatePreferences"/>.
    /// </summary>
    public Error? AssignRole(Role role)
    {
        if (!Enum.IsDefined(role))
        {
            return Error.Validation("Role is invalid.");
        }

        Role = role;
        UpdatedAt = DateTimeOffset.UtcNow;
        return null;
    }

    /// <summary>
    /// This profile's access to <paramref name="area"/>, or <see langword="null"/>
    /// for none. The Owner is deliberately NOT special-cased here: this reports what
    /// was granted, and <c>RolePermissions</c> is the one place that decides what the
    /// Owner may additionally do. Two places answering that would eventually disagree.
    /// </summary>
    public AreaAccess? AccessFor(ContentArea area) =>
        areaPermissions.FirstOrDefault(p => p.Area == area)?.Access;

    /// <summary>
    /// Grants, changes, or (with <paramref name="access"/> null) revokes access to one
    /// area. Revoking removes the row rather than storing a "none", so there is exactly
    /// one representation of no-access.
    /// </summary>
    public Error? SetAreaAccess(ContentArea area, AreaAccess? access)
    {
        if (!Enum.IsDefined(area))
        {
            return Error.Validation("Area is invalid.");
        }

        if (access is not null && !Enum.IsDefined(access.Value))
        {
            return Error.Validation("Access is invalid.");
        }

        var existing = areaPermissions.FirstOrDefault(p => p.Area == area);
        if (access is null)
        {
            if (existing is not null)
            {
                areaPermissions.Remove(existing);
            }
        }
        else if (existing is null)
        {
            areaPermissions.Add(AreaGrant.Create(area, access.Value));
        }
        else
        {
            existing.ChangeAccess(access.Value);
        }

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
