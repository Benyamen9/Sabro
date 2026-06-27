namespace Sabro.API.Logto;

/// <summary>
/// Credentials for the Logto Management API, used to delete a user's identity
/// during account deletion. Bound from the <c>Logto:ManagementApi</c> section.
/// When <see cref="ClientId"/> / <see cref="ClientSecret"/> are blank the
/// management client refuses to act, so account deletion is disabled until the
/// machine-to-machine app is configured.
/// </summary>
public sealed class LogtoManagementOptions
{
    public const string SectionName = "Logto:ManagementApi";

    /// <summary>
    /// The Management API resource indicator. For a self-hosted Logto instance
    /// this is the default tenant's management resource.
    /// </summary>
    public string Resource { get; set; } = "https://default.logto.app/api";

    /// <summary>Machine-to-machine app client id granted Management API access.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Machine-to-machine app client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;
}
