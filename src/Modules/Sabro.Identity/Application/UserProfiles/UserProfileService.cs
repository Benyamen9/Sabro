using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Identity.Domain;
using Sabro.Identity.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Identity.Application.UserProfiles;

internal sealed class UserProfileService : IUserProfileService
{
    private readonly IdentityDbContext dbContext;
    private readonly IValidator<UpdateUserProfileRequest> updateValidator;
    private readonly ILogger<UserProfileService> logger;

    public UserProfileService(
        IdentityDbContext dbContext,
        IValidator<UpdateUserProfileRequest> updateValidator,
        ILogger<UserProfileService> logger)
    {
        this.dbContext = dbContext;
        this.updateValidator = updateValidator;
        this.logger = logger;
    }

    public async Task<Result<UserProfileDto>> GetOrCreateForLogtoUserAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var trimmedLogtoUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedLogtoUserId.Length == 0)
        {
            return Result<UserProfileDto>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var existing = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.LogtoUserId == trimmedLogtoUserId, cancellationToken);
        if (existing is not null)
        {
            return Result<UserProfileDto>.Success(Map(existing));
        }

        var domainResult = UserProfile.Create(trimmedLogtoUserId);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "UserProfile creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<UserProfileDto>.Failure(domainResult.Error!);
        }

        var profile = domainResult.Value!;
        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("UserProfile created. Id={ProfileId} LogtoUserId={LogtoUserId}", profile.Id, profile.LogtoUserId);
        return Result<UserProfileDto>.Success(Map(profile));
    }

    public async Task<Result<UserProfileDto>> UpdateAsync(string logtoUserId, UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var trimmedLogtoUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedLogtoUserId.Length == 0)
        {
            return Result<UserProfileDto>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var shapeResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "UserProfile update rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<UserProfileDto>.Failure(Error.Validation(fields));
        }

        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.LogtoUserId == trimmedLogtoUserId, cancellationToken);
        if (profile is null)
        {
            var domainResult = UserProfile.Create(trimmedLogtoUserId, request.PreferredLanguage, request.PreferredScriptVariant);
            if (!domainResult.IsSuccess)
            {
                logger.LogWarning(
                    "UserProfile creation during update rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                    domainResult.Error!.Code,
                    domainResult.Error.Message);
                return Result<UserProfileDto>.Failure(domainResult.Error!);
            }

            profile = domainResult.Value!;
            var newAccountError = profile.UpdateAccount(request.DisplayName, request.ShowOnLeaderboard);
            if (newAccountError is not null)
            {
                logger.LogWarning(
                    "UserProfile account fields rejected during create-on-update. Code={ErrorCode} Message={ErrorMessage}",
                    newAccountError.Code,
                    newAccountError.Message);
                return Result<UserProfileDto>.Failure(newAccountError);
            }

            dbContext.UserProfiles.Add(profile);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "UserProfile created during update. Id={ProfileId} LogtoUserId={LogtoUserId}",
                profile.Id,
                profile.LogtoUserId);
            return Result<UserProfileDto>.Success(Map(profile));
        }

        var error = profile.UpdatePreferences(request.PreferredLanguage, request.PreferredScriptVariant);
        if (error is not null)
        {
            logger.LogWarning(
                "UserProfile update rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                error.Code,
                error.Message);
            return Result<UserProfileDto>.Failure(error);
        }

        var accountError = profile.UpdateAccount(request.DisplayName, request.ShowOnLeaderboard);
        if (accountError is not null)
        {
            logger.LogWarning(
                "UserProfile account update rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                accountError.Code,
                accountError.Message);
            return Result<UserProfileDto>.Failure(accountError);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "UserProfile updated. Id={ProfileId} LogtoUserId={LogtoUserId}",
            profile.Id,
            profile.LogtoUserId);
        return Result<UserProfileDto>.Success(Map(profile));
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
}
