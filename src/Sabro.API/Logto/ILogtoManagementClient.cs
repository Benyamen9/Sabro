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
}
