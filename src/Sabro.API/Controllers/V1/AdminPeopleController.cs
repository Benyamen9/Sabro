using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
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

    public AdminPeopleController(IUserRoleService userRoles)
    {
        this.userRoles = userRoles;
    }

    /// <summary>Everyone who has signed in, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> List(CancellationToken cancellationToken)
    {
        var logtoUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(logtoUserId))
        {
            return FromError(Error.Validation("Authenticated user is missing a sub claim."));
        }

        var result = await userRoles.ListAsync(logtoUserId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    /// <summary>Grants a role to one person.</summary>
    [HttpPut("{profileId:guid}/role")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> AssignRole(
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
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }
}
