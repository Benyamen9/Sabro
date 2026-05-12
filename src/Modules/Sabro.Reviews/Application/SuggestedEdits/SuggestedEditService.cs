using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Reviews.Domain;
using Sabro.Reviews.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Reviews.Application.SuggestedEdits;

internal sealed class SuggestedEditService : ISuggestedEditService
{
    private readonly ReviewsDbContext dbContext;
    private readonly IValidator<CreateSuggestedEditRequest> createValidator;
    private readonly IUserProfileService userProfiles;
    private readonly ILogger<SuggestedEditService> logger;

    public SuggestedEditService(
        ReviewsDbContext dbContext,
        IValidator<CreateSuggestedEditRequest> createValidator,
        IUserProfileService userProfiles,
        ILogger<SuggestedEditService> logger)
    {
        this.dbContext = dbContext;
        this.createValidator = createValidator;
        this.userProfiles = userProfiles;
        this.logger = logger;
    }

    public async Task<Result<SuggestedEditDto>> ProposeAsync(
        CreateSuggestedEditRequest request,
        string submittedByLogtoUserId,
        CancellationToken cancellationToken)
    {
        var trimmedSubmittedBy = (submittedByLogtoUserId ?? string.Empty).Trim();
        if (trimmedSubmittedBy.Length == 0)
        {
            return Result<SuggestedEditDto>.Failure(Error.Validation("SubmittedByLogtoUserId is required."));
        }

        var shapeResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            logger.LogWarning(
                "SuggestedEdit proposal rejected at request validation. Fields={FieldNames}",
                fields.Keys);
            return Result<SuggestedEditDto>.Failure(Error.Validation(fields));
        }

        var roleResult = await userProfiles.GetOrCreateForLogtoUserAsync(trimmedSubmittedBy, cancellationToken);
        if (!roleResult.IsSuccess)
        {
            return Result<SuggestedEditDto>.Failure(roleResult.Error!);
        }

        if (roleResult.Value!.Role != Role.ExpertReviewer)
        {
            logger.LogWarning(
                "SuggestedEdit proposal forbidden. SubmittedBy={SubmittedBy} ActualRole={Role}",
                trimmedSubmittedBy,
                roleResult.Value.Role);
            return Result<SuggestedEditDto>.Failure(Error.Forbidden("Only Expert Reviewers may propose edits."));
        }

        var domainResult = SuggestedEdit.Create(
            request.TargetType,
            request.TargetId,
            request.TargetVersion,
            request.ProposedContent,
            trimmedSubmittedBy,
            request.Rationale);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "SuggestedEdit creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<SuggestedEditDto>.Failure(domainResult.Error!);
        }

        var edit = domainResult.Value!;
        dbContext.SuggestedEdits.Add(edit);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "SuggestedEdit proposed. Id={EditId} TargetType={TargetType} TargetId={TargetId}",
            edit.Id,
            edit.TargetType,
            edit.TargetId);
        return Result<SuggestedEditDto>.Success(Map(edit));
    }

    public async Task<Result<SuggestedEditDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var edit = await dbContext.SuggestedEdits
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return edit is null
            ? Result<SuggestedEditDto>.Failure(Error.NotFound($"SuggestedEdit {id} not found."))
            : Result<SuggestedEditDto>.Success(Map(edit));
    }

    public async Task<Result<PagedResult<SuggestedEditDto>>> ListAsync(
        SuggestedEditListFilters filters,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<SuggestedEditDto>>.Failure(pageError);
        }

        var query = dbContext.SuggestedEdits.AsNoTracking();

        if (filters.Status is not null)
        {
            var status = filters.Status.Value;
            query = query.Where(e => e.Status == status);
        }

        if (filters.TargetType is not null)
        {
            var targetType = filters.TargetType.Value;
            query = query.Where(e => e.TargetType == targetType);
        }

        if (filters.TargetId is not null)
        {
            var targetId = filters.TargetId.Value;
            query = query.Where(e => e.TargetId == targetId);
        }

        var trimmedSubmittedBy = string.IsNullOrWhiteSpace(filters.SubmittedByLogtoUserId)
            ? null
            : filters.SubmittedByLogtoUserId.Trim();
        if (trimmedSubmittedBy is not null)
        {
            query = query.Where(e => e.SubmittedByLogtoUserId == trimmedSubmittedBy);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(Map).ToArray();
        return Result<PagedResult<SuggestedEditDto>>.Success(
            new PagedResult<SuggestedEditDto>(mapped, total, page, pageSize));
    }

    public Task<Result<SuggestedEditDto>> AcceptAsync(
        Guid id,
        DecisionRequest request,
        string decidedByLogtoUserId,
        CancellationToken cancellationToken) =>
        ApplyDecisionAsync(id, request, decidedByLogtoUserId, accept: true, cancellationToken);

    public Task<Result<SuggestedEditDto>> RejectAsync(
        Guid id,
        DecisionRequest request,
        string decidedByLogtoUserId,
        CancellationToken cancellationToken) =>
        ApplyDecisionAsync(id, request, decidedByLogtoUserId, accept: false, cancellationToken);

    private static SuggestedEditDto Map(SuggestedEdit edit) => new(
        edit.Id,
        edit.TargetType,
        edit.TargetId,
        edit.TargetVersion,
        edit.ProposedContent,
        edit.Rationale,
        edit.SubmittedByLogtoUserId,
        edit.Status,
        edit.DecisionByLogtoUserId,
        edit.DecisionAt,
        edit.DecisionNote,
        edit.CreatedAt,
        edit.UpdatedAt);

    private async Task<Result<SuggestedEditDto>> ApplyDecisionAsync(
        Guid id,
        DecisionRequest request,
        string decidedByLogtoUserId,
        bool accept,
        CancellationToken cancellationToken)
    {
        var trimmedDecidedBy = (decidedByLogtoUserId ?? string.Empty).Trim();
        if (trimmedDecidedBy.Length == 0)
        {
            return Result<SuggestedEditDto>.Failure(Error.Validation("DecisionByLogtoUserId is required."));
        }

        var roleResult = await userProfiles.GetOrCreateForLogtoUserAsync(trimmedDecidedBy, cancellationToken);
        if (!roleResult.IsSuccess)
        {
            return Result<SuggestedEditDto>.Failure(roleResult.Error!);
        }

        if (roleResult.Value!.Role != Role.Owner)
        {
            logger.LogWarning(
                "SuggestedEdit decision forbidden. DecidedBy={DecidedBy} ActualRole={Role} Accept={Accept}",
                trimmedDecidedBy,
                roleResult.Value.Role,
                accept);
            return Result<SuggestedEditDto>.Failure(Error.Forbidden("Only the Owner may accept or reject suggestions."));
        }

        var edit = await dbContext.SuggestedEdits
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (edit is null)
        {
            return Result<SuggestedEditDto>.Failure(Error.NotFound($"SuggestedEdit {id} not found."));
        }

        var domainError = accept
            ? edit.Accept(trimmedDecidedBy, request.Note)
            : edit.Reject(trimmedDecidedBy, request.Note);
        if (domainError is not null)
        {
            return Result<SuggestedEditDto>.Failure(domainError);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "SuggestedEdit decided. Id={EditId} Status={Status} DecidedBy={DecidedBy}",
            edit.Id,
            edit.Status,
            edit.DecisionByLogtoUserId);
        return Result<SuggestedEditDto>.Success(Map(edit));
    }
}
