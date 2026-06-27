using Sabro.Shared.Results;

namespace Sabro.Identity.Application.UserProfiles;

public interface IUserProfileService
{
    /// <summary>
    /// Returns the caller's profile, creating a default one (English,
    /// Estrangela) on first call. Idempotent.
    /// </summary>
    Task<Result<UserProfileDto>> GetOrCreateForLogtoUserAsync(string logtoUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the caller's preferences. Creates the profile first if it does
    /// not yet exist — matches the GET semantics so the frontend never has to
    /// distinguish "new user" from "returning user".
    /// </summary>
    Task<Result<UserProfileDto>> UpdateAsync(string logtoUserId, UpdateUserProfileRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes the caller's profile row. Idempotent: succeeds with
    /// <c>false</c> when there is no profile to delete, <c>true</c> when one was
    /// removed. Used by account deletion (right to erasure).
    /// </summary>
    Task<Result<bool>> DeleteAsync(string logtoUserId, CancellationToken cancellationToken);
}
