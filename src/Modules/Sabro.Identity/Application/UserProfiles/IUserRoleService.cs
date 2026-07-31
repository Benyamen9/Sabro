using Sabro.Identity.Domain;
using Sabro.Shared.Results;

namespace Sabro.Identity.Application.UserProfiles;

/// <summary>
/// Reading and granting roles — the write side of "who may edit what".
/// Separate from <see cref="IUserProfileService"/>, which is about a person
/// managing their own profile; this is about the Owner managing everyone's.
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Every profile, newest first. The list the People page renders.
    /// </summary>
    Task<Result<IReadOnlyList<UserProfileDto>>> ListAsync(string callerLogtoUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Grants <paramref name="role"/> to the profile identified by
    /// <paramref name="targetProfileId"/>.
    /// </summary>
    /// <remarks>
    /// Refuses to let the caller change their own role. Without that, the only
    /// Owner can demote themselves and leave the installation with nobody able to
    /// grant roles — recoverable only by editing the database by hand, which is
    /// exactly the situation this endpoint exists to end.
    /// </remarks>
    Task<Result<UserProfileDto>> AssignRoleAsync(
        string callerLogtoUserId,
        Guid targetProfileId,
        Role role,
        CancellationToken cancellationToken);
}
