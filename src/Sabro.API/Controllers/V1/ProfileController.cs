using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.API.Logto;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Play.Application.GameResults;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The authenticated caller's own profile — the hub's "my profile" surface.
/// Identity owns who the user is; play activity lives in the Play module.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/profile")]
public sealed class ProfileController : ApiControllerBase
{
    /// <summary>
    /// Names what the export covers. Identity-provider data (email, password,
    /// social links) is held by Logto, not Sabro, so it is out of scope here.
    /// </summary>
    private const string ExportScope =
        "All personal data stored by Sabro: your profile and your game results. "
        + "Sign-in identity data (email, password, linked social accounts) is managed by the identity provider and is not included.";

    private readonly IUserProfileService userProfileService;
    private readonly IGameResultService gameResultService;
    private readonly ILogtoManagementClient logtoManagementClient;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ProfileController> logger;

    public ProfileController(
        IUserProfileService userProfileService,
        IGameResultService gameResultService,
        ILogtoManagementClient logtoManagementClient,
        TimeProvider timeProvider,
        ILogger<ProfileController> logger)
    {
        this.userProfileService = userProfileService;
        this.gameResultService = gameResultService;
        this.logtoManagementClient = logtoManagementClient;
        this.timeProvider = timeProvider;
        this.logger = logger;
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

    /// <summary>
    /// Returns everything Sabro stores about the caller in one portable JSON
    /// document: their profile and all their game results. Right to data
    /// portability / right of access (GDPR). Uses the write scope like the
    /// other personal-data surfaces (results/me, account deletion).
    /// </summary>
    [HttpGet("me/export")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(ProfileExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileExportDto>> ExportMe(CancellationToken cancellationToken)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var userId = logtoUserIdResult.Value!;

        var profileResult = await userProfileService.GetOrCreateForLogtoUserAsync(userId, cancellationToken);
        if (!profileResult.IsSuccess)
        {
            return FromError(profileResult.Error!);
        }

        var resultsResult = await gameResultService.ListAllForUserAsync(userId, cancellationToken);
        if (!resultsResult.IsSuccess)
        {
            return FromError(resultsResult.Error!);
        }

        logger.LogInformation("Personal-data export served. ResultCount={ResultCount}", resultsResult.Value!.Count);
        return Ok(new ProfileExportDto(
            timeProvider.GetUtcNow(),
            ExportScope,
            profileResult.Value!,
            resultsResult.Value!));
    }

    /// <summary>
    /// Permanently deletes the caller's account: their play results, their
    /// profile, and finally their Logto identity. The identity is removed last
    /// so that a failure there leaves the caller still able to sign in and
    /// retry. Right to erasure (GDPR).
    /// </summary>
    [HttpDelete("me")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        var logtoUserIdResult = ResolveLogtoUserId();
        if (!logtoUserIdResult.IsSuccess)
        {
            return FromError(logtoUserIdResult.Error!);
        }

        var userId = logtoUserIdResult.Value!;

        var resultsResult = await gameResultService.DeleteAllForUserAsync(userId, cancellationToken);
        if (!resultsResult.IsSuccess)
        {
            return FromError(resultsResult.Error!);
        }

        var profileResult = await userProfileService.DeleteAsync(userId, cancellationToken);
        if (!profileResult.IsSuccess)
        {
            return FromError(profileResult.Error!);
        }

        var identityResult = await logtoManagementClient.DeleteUserAsync(userId, cancellationToken);
        if (!identityResult.IsSuccess)
        {
            return FromError(identityResult.Error!);
        }

        logger.LogInformation("Account deleted. ResultsRemoved={ResultsRemoved}", resultsResult.Value);
        return NoContent();
    }
}
