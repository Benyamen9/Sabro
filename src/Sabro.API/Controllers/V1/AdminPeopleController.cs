using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.API.Logto;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Shared.Results;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Who may edit what. Lists the people who have signed in and grants each of them
/// a role.
/// </summary>
/// <remarks>
/// <para>
/// Before this existed there was no way at all to change a role: <c>AssignRole</c>
/// was never called from anywhere, so the only route was editing the database by
/// hand.
/// </para>
/// <para>
/// Two gates, deliberately. The <c>api:v1:admin</c> scope decides whether a
/// request may reach an admin endpoint at all; the caller's Owner role decides
/// whether they may manage other people. Holding the scope is not enough — being
/// trusted with content is not the same as being trusted with who else gets in.
/// </para>
/// </remarks>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/people")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminPeopleController : ApiControllerBase
{
    private readonly IUserRoleService userRoles;
    private readonly ILogtoManagementClient logtoManagement;

    public AdminPeopleController(IUserRoleService userRoles, ILogtoManagementClient logtoManagement)
    {
        this.userRoles = userRoles;
        this.logtoManagement = logtoManagement;
    }

    /// <summary>
    /// Everyone who has signed in, newest first, with names resolved from Logto
    /// where possible.
    /// </summary>
    /// <remarks>
    /// Sabro stores no name or email, so a bare profile list is a column of opaque
    /// ids — unusable for deciding who to trust with what. Identities are read from
    /// Logto per request, shown, and discarded. If that lookup yields nothing the
    /// list still renders and roles are still grantable.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PersonDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PersonDto>>> List(CancellationToken cancellationToken)
    {
        var logtoUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(logtoUserId))
        {
            return FromError(Error.Validation("Authenticated user is missing a sub claim."));
        }

        var result = await userRoles.ListAsync(logtoUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        var people = await ToPeopleAsync(result.Value!, logtoUserId, cancellationToken);
        return Ok(people);
    }

    /// <summary>Grants a role to one person.</summary>
    [HttpPut("{profileId:guid}/role")]
    [ProducesResponseType(typeof(PersonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonDto>> AssignRole(
        Guid profileId,
        AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var logtoUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(logtoUserId))
        {
            return FromError(Error.Validation("Authenticated user is missing a sub claim."));
        }

        var result = await userRoles.AssignRoleAsync(logtoUserId, profileId, request.Role, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        // Same shape as the list, deliberately. The page swaps this row straight
        // into the table it is showing; returning the bare profile dropped the name
        // and email (which live only in Logto) so the row went blank on save, and
        // leaked the opaque Logto id the list is careful never to send.
        var people = await ToPeopleAsync([result.Value!], logtoUserId, cancellationToken);
        return Ok(people[0]);
    }

    /// <summary>
    /// Turns profiles into people by asking Logto who they are. One path for both
    /// endpoints, so a list row and a freshly-saved row can never disagree.
    /// </summary>
    private async Task<IReadOnlyList<PersonDto>> ToPeopleAsync(
        IReadOnlyList<UserProfileDto> profiles,
        string callerLogtoUserId,
        CancellationToken cancellationToken)
    {
        var identities = await logtoManagement.GetUserIdentitiesAsync(
            profiles.Select(p => p.LogtoUserId).ToArray(),
            cancellationToken);

        return profiles
            .Select(profile =>
            {
                identities.TryGetValue(profile.LogtoUserId, out var identity);
                return new PersonDto(
                    profile.Id,
                    profile.Role,
                    profile.DisplayName,
                    identity?.Name,
                    identity?.Email,
                    profile.CreatedAt,
                    string.Equals(profile.LogtoUserId, callerLogtoUserId, StringComparison.Ordinal));
            })
            .ToArray();
    }
}
