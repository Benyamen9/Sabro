namespace Sabro.API.Logto;

/// <summary>
/// Just enough of a Logto identity to recognise a person on the People page.
/// </summary>
/// <remarks>
/// Read from Logto at render time and <b>never stored</b>. Sabro's own
/// <c>UserProfile</c> deliberately holds no name and no email — Logto stays the
/// single source of truth for identity — and this type exists to display, not to
/// mirror. Nothing here is persisted or written to a log.
/// </remarks>
public sealed record LogtoUserIdentity(string Id, string? Name, string? Email);
