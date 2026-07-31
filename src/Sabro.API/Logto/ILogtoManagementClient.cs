using Sabro.Shared.Results;

namespace Sabro.API.Logto;

public interface ILogtoManagementClient
{
    /// <summary>
    /// Permanently deletes the Logto identity for <paramref name="logtoUserId"/>
    /// via the Management API. Idempotent: a 404 (already gone) is treated as
    /// success. Fails when the Management API is not configured or the call
    /// errors, so the caller can stop and surface the problem.
    /// </summary>
    Task<Result<bool>> DeleteUserAsync(string logtoUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Looks up the display identities for <paramref name="logtoUserIds"/>, so the
    /// People page can show who somebody is rather than an opaque id.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Never fails the caller.</b> Returns whatever it could resolve and omits
    /// the rest: an unconfigured Management API, a network error, or a user Logto
    /// no longer knows all yield a smaller map, not an error. Identity here is a
    /// nicety for recognising people — it must never become load-bearing for
    /// authorisation, and a page that cannot grant a role because a lookup timed
    /// out would be a worse failure than one showing a bare id.
    /// </para>
    /// <para>
    /// Results are for display only and are not persisted anywhere.
    /// </para>
    /// </remarks>
    Task<IReadOnlyDictionary<string, LogtoUserIdentity>> GetUserIdentitiesAsync(
        IReadOnlyCollection<string> logtoUserIds,
        CancellationToken cancellationToken);
}
