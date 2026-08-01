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
        //
        // Except during bootstrap. That reasoning presupposes an Owner exists; when
        // none does, refusing self-assignment blocks the one action the bootstrap
        // clause was written to allow — appointing the first Owner — and leaves the
        // installation exactly as stuck as before. The first shipped build hit this
        // in production: every profile read "can only play", and the sole person who
        // could fix it was the one person the rule forbade from doing so.
        if (string.Equals(target.LogtoUserId, caller.Value!.LogtoUserId, StringComparison.Ordinal))
        {
            var ownerExists = await dbContext.UserProfiles
                .AnyAsync(p => p.Role == Role.Owner, cancellationToken);
            if (ownerExists)
            {
                return Result<UserProfileDto>.Failure(
                    Error.Validation("You cannot change your own role."));
            }

            if (role != Role.Owner)
            {
                // Bootstrap exists to create an Owner, not as a general licence to
                // edit your own permissions while none exists.
                return Result<UserProfileDto>.Failure(
                    Error.Validation("With no Owner set, you may only appoint yourself Owner."));
            }
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

    public async Task<Result<UserProfileDto>> SetAreaAccessAsync(
        string callerLogtoUserId,
        Guid targetProfileId,
        ContentArea area,
        AreaAccess? access,
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

        // No self-assignment guard here, unlike AssignRoleAsync. That rule exists so
        // the sole Owner cannot strand the installation by demoting themselves; an
        // area grant cannot strand anything, because the Owner role — the thing that
        // grants access to everything, including this endpoint — is untouched by it.
        var error = target.SetAreaAccess(area, access);
        if (error is not null)
        {
            return Result<UserProfileDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Privileged and security-relevant, so recorded. The profile id is not
        // personal data and the Logto id is deliberately not written here.
        logger.LogInformation(
            "UserProfile area access set. TargetProfileId={TargetProfileId} Area={Area} Access={Access}",
            target.Id,
            area,
            access?.ToString() ?? "none");

        return Result<UserProfileDto>.Success(Map(target));
    }

    /// <summary>
    /// Area grants, ordered by area so the payload is stable between requests —
    /// an unordered child collection makes diffs and tests noisy for no reason.
    /// </summary>
    private static AreaGrantDto[] MapAreas(UserProfile profile) =>
        profile.AreaPermissions
            .OrderBy(a => a.Area)
            .Select(a => new AreaGrantDto(a.Area, a.Access))
            .ToArray();

    private static UserProfileDto Map(UserProfile profile) => new(
        profile.Id,
        profile.LogtoUserId,
        profile.PreferredLanguage,
        profile.PreferredScriptVariant,
        profile.Role,
        MapAreas(profile),
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

        if (RolePermissions.CanAssignRoles(caller))
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
