using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Identity.Application.UserProfiles;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The authenticated caller's own profile — the hub's "my profile" surface.
/// Identity owns who the user is; play activity lives in the Play module.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/profile")]
public sealed class ProfileController : ApiControllerBase
{
    private readonly IUserProfileService userProfileService;

    public ProfileController(IUserProfileService userProfileService)
    {
        this.userProfileService = userProfileService;
    }

    /// <summary>
    /// Returns the caller's profile, auto-creating a default one (English,
    /// Estrangela) on first call. Read scope is sufficient — even a read-only
    /// client needs a profile to know which language/script to render.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var result = await userProfileService.GetOrCreateForLogtoUserAsync(logtoUserIdResult.Value!, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Replaces the caller's editable preferences (preferred language and
    /// default script variant). A full representation is sent, so this is a
    /// PUT rather than a PATCH.
    /// </summary>
    [HttpPut("me")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var result = await userProfileService.UpdateAsync(logtoUserIdResult.Value!, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
