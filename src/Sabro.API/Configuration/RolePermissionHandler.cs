using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Sabro.Identity.Application.UserProfiles;

namespace Sabro.API.Configuration;

/// <summary>
/// Resolves the caller's profile and applies a <see cref="RolePermissionRequirement"/>.
/// </summary>
/// <remarks>
/// Fails closed. A missing subject claim, an unresolvable profile, or a role that
/// does not satisfy the predicate all leave the requirement unmet, which ASP.NET
/// turns into a 403. Nothing here succeeds by omission.
/// </remarks>
internal sealed class RolePermissionHandler : AuthorizationHandler<RolePermissionRequirement>
{
    private readonly IUserProfileService userProfiles;
    private readonly ILogger<RolePermissionHandler> logger;

    public RolePermissionHandler(IUserProfileService userProfiles, ILogger<RolePermissionHandler> logger)
    {
        this.userProfiles = userProfiles;
        this.logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RolePermissionRequirement requirement)
    {
        var logtoUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(logtoUserId))
        {
            return;
        }

        // Get-or-create, matching every other read of the caller's own profile: a
        // first-time admin has no row yet, and refusing them here would be a
        // confusing 403 rather than the plain "your role does not allow this".
        var profile = await userProfiles.GetOrCreateForLogtoUserAsync(logtoUserId, CancellationToken.None);
        if (!profile.IsSuccess)
        {
            logger.LogWarning("Could not resolve a profile while authorising {Requirement}.", requirement.Description);
            return;
        }

        if (requirement.IsAllowed(profile.Value!.Role))
        {
            context.Succeed(requirement);
        }
    }
}
