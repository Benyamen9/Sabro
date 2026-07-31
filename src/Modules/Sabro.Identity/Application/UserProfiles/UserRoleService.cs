using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Identity.Domain;
using Sabro.Identity.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Identity.Application.UserProfiles;

internal sealed class UserRoleService : IUserRoleService
{
    private readonly IdentityDbContext dbContext;
    private readonly ILogger<UserRoleService> logger;

    public UserRoleService(IdentityDbContext dbContext, ILogger<UserRoleService> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<Result<IReadOnlyList<UserProfileDto>>> ListAsync(
        string callerLogtoUserId,
        CancellationToken cancellationToken)
    {
        var caller = await AuthoriseAsync(callerLogtoUserId, cancellationToken);
        if (!caller.IsSuccess)
        {
            return Result<IReadOnlyList<UserProfileDto>>.Failure(caller.Error!);
        }

        var profiles = await dbContext.UserProfiles
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserProfileDto>>.Success(profiles.Select(Map).ToArray());
    }

    public async Task<Result<UserProfileDto>> AssignRoleAsync(
        string callerLogtoUserId,
        Guid targetProfileId,
        Role role,
        CancellationToken cancellationToken)
    {
        var caller = await AuthoriseAsync(callerLogtoUserId, cancellationToken);
        if (!caller.IsSuccess)
        {
            return Result<UserProfileDto>.Failure(caller.Error!);
        }

        var target = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.Id == targetProfileId, cancellationToken);
        if (target is null)
        {
            return Result<UserProfileDto>.Failure(Error.NotFound($"UserProfile {targetProfileId} not found."));
        }

        // Changing your own role is refused rather than merely discouraged: the
        // sole Owner demoting themselves leaves nobody able to grant roles, and the
        // only way back is editing the database by hand — the very thing this
        // endpoint exists to replace.
        if (string.Equals(target.LogtoUserId, caller.Value!.LogtoUserId, StringComparison.Ordinal))
        {
            return Result<UserProfileDto>.Failure(
                Error.Validation("You cannot change your own role."));
        }

        var error = target.AssignRole(role);
        if (error is not null)
        {
            return Result<UserProfileDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // A grant is a privileged, rare, security-relevant act — exactly what the
        // logging rules say to record. The profile id is not personal data; the
        // Logto id is not written here.
        logger.LogInformation(
            "UserProfile role assigned. TargetProfileId={TargetProfileId} Role={Role}",
            target.Id,
            role);

        return Result<UserProfileDto>.Success(Map(target));
    }

    private static UserProfileDto Map(UserProfile profile) => new(
        profile.Id,
        profile.LogtoUserId,
        profile.PreferredLanguage,
        profile.PreferredScriptVariant,
        profile.Role,
        profile.DisplayName,
        profile.ShowOnLeaderboard,
        profile.CreatedAt,
        profile.UpdatedAt);

    /// <summary>
    /// Only an Owner may read or change roles.
    /// </summary>
    /// <remarks>
    /// With one deliberate exception: when <b>no</b> Owner exists yet, any caller
    /// who already holds the admin scope is allowed through, once, so they can
    /// appoint one. Without it there is a genuine deadlock — granting Owner
    /// requires being Owner — and the only escape is hand-editing the database,
    /// which is how the role ended up unmanaged in the first place. The clause
    /// closes itself the moment an Owner exists, and it grants nothing beyond
    /// what the admin scope already implies today.
    /// </remarks>
    private async Task<Result<UserProfile>> AuthoriseAsync(string callerLogtoUserId, CancellationToken cancellationToken)
    {
        var trimmed = (callerLogtoUserId ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return Result<UserProfile>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var caller = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.LogtoUserId == trimmed, cancellationToken);
        if (caller is null)
        {
            return Result<UserProfile>.Failure(Error.Forbidden("Only the Owner may manage roles."));
        }

        if (RolePermissions.CanAssignRoles(caller.Role))
        {
            return Result<UserProfile>.Success(caller);
        }

        var anyOwnerExists = await dbContext.UserProfiles
            .AnyAsync(p => p.Role == Role.Owner, cancellationToken);
        if (!anyOwnerExists)
        {
            logger.LogWarning(
                "Role management reached with no Owner present; allowing the admin-scoped caller to appoint one. CallerProfileId={CallerProfileId}",
                caller.Id);

            return Result<UserProfile>.Success(caller);
        }

        return Result<UserProfile>.Failure(Error.Forbidden("Only the Owner may manage roles."));
    }
}
