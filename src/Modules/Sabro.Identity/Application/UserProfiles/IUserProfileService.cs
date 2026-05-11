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
}
