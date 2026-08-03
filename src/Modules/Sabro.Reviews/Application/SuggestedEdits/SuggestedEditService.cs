using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Reviews.Domain;
using Sabro.Reviews.Infrastructure;
using Sabro.Shared.Abstractions;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Reviews.Application.SuggestedEdits;

internal sealed class SuggestedEditService : ISuggestedEditService
{
    private readonly ReviewsDbContext dbContext;
    private readonly IValidator<CreateSuggestedEditRequest> createValidator;
    private readonly IValidator<CreateFieldProposalRequest> fieldProposalValidator;
    private readonly IEnumerable<IProposalTargetSource> targetSources;
    private readonly IUserProfileService userProfiles;
    private readonly ILogger<SuggestedEditService> logger;

    public SuggestedEditService(
        ReviewsDbContext dbContext,
        IValidator<CreateSuggestedEditRequest> createValidator,
        IValidator<CreateFieldProposalRequest> fieldProposalValidator,
        IEnumerable<IProposalTargetSource> targetSources,
        IUserProfileService userProfiles,
        ILogger<SuggestedEditService> logger)
    {
        this.dbContext = dbContext;
        this.createValidator = createValidator;
        this.fieldProposalValidator = fieldProposalValidator;
        this.targetSources = targetSources;
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

        if (!RolePermissions.CanProposeTranslationEdit(roleResult.Value!))
        {
            logger.LogWarning(
                "SuggestedEdit proposal forbidden. SubmittedBy={SubmittedBy} ActualRole={Role}",
                trimmedSubmittedBy,
                roleResult.Value!.Role);
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

    public async Task<Result<SuggestedEditDto>> ProposeFieldChangeAsync(
        CreateFieldProposalRequest request,
        string submittedByLogtoUserId,
        CancellationToken cancellationToken)
    {
        var trimmedSubmittedBy = (submittedByLogtoUserId ?? string.Empty).Trim();
        if (trimmedSubmittedBy.Length == 0)
        {
            return Result<SuggestedEditDto>.Failure(Error.Validation("SubmittedByLogtoUserId is required."));
        }

        var shapeResult = await fieldProposalValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            logger.LogWarning(
                "Field proposal rejected at request validation. Fields={FieldNames}",
                fields.Keys);
            return Result<SuggestedEditDto>.Failure(Error.Validation(fields));
        }

        var roleResult = await userProfiles.GetOrCreateForLogtoUserAsync(trimmedSubmittedBy, cancellationToken);
        if (!roleResult.IsSuccess)
        {
            return Result<SuggestedEditDto>.Failure(roleResult.Error!);
        }

        var mayPropose = PermissionFor(request.TargetType);
        if (mayPropose is null || !mayPropose(roleResult.Value!))
        {
            logger.LogWarning(
                "Field proposal forbidden. SubmittedBy={SubmittedBy} ActualRole={Role} TargetType={TargetType}",
                trimmedSubmittedBy,
                roleResult.Value!.Role,
                request.TargetType);
            return Result<SuggestedEditDto>.Failure(
                Error.Forbidden("You may not propose changes to this kind of content."));
        }

        // The module that owns the target decides both whether it exists and which of
        // its fields may be proposed against. Reviews deliberately knows neither.
        var source = targetSources.FirstOrDefault(s => s.TargetTypeName == request.TargetType.ToString());
        if (source is null)
        {
            return Result<SuggestedEditDto>.Failure(
                Error.Validation($"{request.TargetType} does not accept field proposals."));
        }

        var field = request.Field.Trim();
        if (!source.ProposableFields.Contains(field, StringComparer.Ordinal))
        {
            // Deliberately says which fields ARE allowed: the caller is a trusted
            // reviewer using the backoffice, and a bare "invalid field" turns a typo
            // into a support question. Nothing here is sensitive — it is the shape of
            // the edit form they are already looking at.
            logger.LogWarning(
                "Field proposal rejected: {Field} is not proposable on {TargetType}.",
                field,
                request.TargetType);
            return Result<SuggestedEditDto>.Failure(Error.Validation(
                $"'{field}' cannot be proposed on {request.TargetType}. Allowed: {string.Join(", ", source.ProposableFields)}."));
        }

        var targetUpdatedAt = await source.GetUpdatedAtAsync(request.TargetId, cancellationToken);
        if (targetUpdatedAt is null)
        {
            return Result<SuggestedEditDto>.Failure(
                Error.NotFound($"{request.TargetType} {request.TargetId} not found."));
        }

        // The "before" half of the diff, and the value staleness is later judged against.
        var originalValue = await source.GetFieldValueAsync(request.TargetId, field, cancellationToken);

        var domainResult = SuggestedEdit.ProposeFieldChange(
            request.TargetType,
            request.TargetId,
            field,
            request.ProposedValue,
            originalValue,
            targetUpdatedAt.Value,
            trimmedSubmittedBy,
            request.Rationale);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "Field proposal rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<SuggestedEditDto>.Failure(domainResult.Error!);
        }

        var proposal = domainResult.Value!;
        dbContext.SuggestedEdits.Add(proposal);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Field proposal filed. Id={EditId} TargetType={TargetType} TargetId={TargetId} Field={Field}",
            proposal.Id,
            proposal.TargetType,
            proposal.TargetId,
            proposal.Field);
        return Result<SuggestedEditDto>.Success(Map(proposal));
    }

    public Result<IReadOnlyCollection<string>> GetProposableFields(SuggestedEditTargetType targetType)
    {
        var source = targetSources.FirstOrDefault(s => s.TargetTypeName == targetType.ToString());
        return source is null
            ? Result<IReadOnlyCollection<string>>.Failure(
                Error.Validation($"{targetType} does not accept field proposals."))
            : Result<IReadOnlyCollection<string>>.Success(source.ProposableFields);
    }

    public async Task<Result<SuggestedEditDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var edit = await dbContext.SuggestedEdits
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (edit is null)
        {
            return Result<SuggestedEditDto>.Failure(Error.NotFound($"SuggestedEdit {id} not found."));
        }

        var labels = await ResolveLabelsAsync([edit], cancellationToken);
        return Result<SuggestedEditDto>.Success(Map(edit, LabelFor(labels, edit)));
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

        var labels = await ResolveLabelsAsync(items, cancellationToken);
        var mapped = items.Select(edit => Map(edit, LabelFor(labels, edit))).ToArray();
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

    /// <summary>
    /// Which role may propose against a target type. One expression rather than a
    /// chain of conditionals, so adding a proposable area is an entry here plus a
    /// predicate in <see cref="RolePermissions"/> — nothing scattered.
    /// </summary>
    private static Func<IAccessProfile, bool>? PermissionFor(SuggestedEditTargetType targetType) => targetType switch
    {
        SuggestedEditTargetType.LexiconEntry =>
            p => RolePermissions.CanPropose(p, ContentArea.Lexicon),
        SuggestedEditTargetType.HistoricalFigure =>
            p => RolePermissions.CanPropose(p, ContentArea.Shmo),
        SuggestedEditTargetType.Segment or SuggestedEditTargetType.Annotation =>
            RolePermissions.CanProposeTranslationEdit,
        _ => null,
    };

    private static SuggestedEditDto Map(SuggestedEdit edit, ProposalTargetLabel? label = null) => new(
        edit.Id,
        edit.TargetType,
        edit.TargetId,
        edit.TargetVersion,
        edit.TargetUpdatedAt,
        edit.Field,
        edit.OriginalValue,
        edit.AcceptedDespiteChange,
        edit.ProposedContent,
        edit.Rationale,
        edit.SubmittedByLogtoUserId,
        edit.Status,
        edit.DecisionByLogtoUserId,
        edit.DecisionAt,
        edit.DecisionNote,
        edit.CreatedAt,
        edit.UpdatedAt,
        label);

    private static ProposalTargetLabel? LabelFor(
        IReadOnlyDictionary<(SuggestedEditTargetType TargetType, Guid TargetId), ProposalTargetLabel> labels,
        SuggestedEdit edit) =>
        labels.TryGetValue((edit.TargetType, edit.TargetId), out var label) ? label : null;

    /// <summary>
    /// Hands the accepted value to the module that owns the target, which writes it
    /// through its own normal write path.
    /// </summary>
    /// <remarks>
    /// Reviews never writes another module's content itself. Everything that guards a
    /// backoffice save — validation, NFC normalisation, the publication rules, the
    /// Meilisearch reindex — has to guard this too, and the only way to be sure of
    /// that is to go through the same service the form posts to.
    /// </remarks>
    private async Task<Error?> ApplyToTargetAsync(SuggestedEdit edit, CancellationToken cancellationToken)
    {
        var source = targetSources.FirstOrDefault(s => s.TargetTypeName == edit.TargetType.ToString());
        if (source is null)
        {
            return Error.Validation(
                $"{edit.TargetType} proposals cannot be applied: no module claims that target type.");
        }

        return await source.ApplyFieldAsync(
            edit.TargetId,
            edit.Field!,
            edit.ProposedContent,
            cancellationToken);
    }

    /// <summary>
    /// Names every target on a page of proposals, so the queue can say which word or
    /// which figure each one is about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One call per module rather than one per row: a page of twenty proposals across
    /// two areas costs two queries. Grouping by target type is what makes that
    /// possible, and it is why the interface takes a collection.
    /// </para>
    /// <para>
    /// A target with no label — deleted since, or owned by a module no longer
    /// registered — is simply absent. The queue then shows the proposal without a
    /// name, which is what it did for every row before this existed; losing the label
    /// must never lose the proposal.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyDictionary<(SuggestedEditTargetType TargetType, Guid TargetId), ProposalTargetLabel>>
        ResolveLabelsAsync(List<SuggestedEdit> edits, CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<(SuggestedEditTargetType, Guid), ProposalTargetLabel>();
        if (edits.Count == 0)
        {
            return resolved;
        }

        foreach (var group in edits.GroupBy(edit => edit.TargetType))
        {
            var source = targetSources.FirstOrDefault(s => s.TargetTypeName == group.Key.ToString());
            if (source is null)
            {
                continue;
            }

            var ids = group.Select(edit => edit.TargetId).Distinct().ToArray();
            var labels = await source.GetLabelsAsync(ids, cancellationToken);
            foreach (var (id, label) in labels)
            {
                resolved[(group.Key, id)] = label;
            }
        }

        return resolved;
    }

    /// <summary>
    /// True when the proposed field's current value differs from what it held when the
    /// proposal was filed. A target that has since been deleted counts as changed.
    /// </summary>
    private async Task<Result<bool>> DetectFieldChangeAsync(
        SuggestedEdit edit,
        CancellationToken cancellationToken)
    {
        var source = targetSources.FirstOrDefault(s => s.TargetTypeName == edit.TargetType.ToString());
        if (source is null)
        {
            // The module that owns this target is no longer registered, so nothing can
            // vouch for the content. Refusing beats guessing.
            return Result<bool>.Failure(Error.Validation(
                $"{edit.TargetType} proposals cannot be decided: no module claims that target type."));
        }

        if (await source.GetUpdatedAtAsync(edit.TargetId, cancellationToken) is null)
        {
            return Result<bool>.Failure(Error.NotFound(
                $"{edit.TargetType} {edit.TargetId} no longer exists."));
        }

        var current = await source.GetFieldValueAsync(edit.TargetId, edit.Field!, cancellationToken);
        return Result<bool>.Success(!string.Equals(current, edit.OriginalValue, StringComparison.Ordinal));
    }

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

        if (!RolePermissions.CanDecideProposals(roleResult.Value!))
        {
            logger.LogWarning(
                "SuggestedEdit decision forbidden. DecidedBy={DecidedBy} ActualRole={Role} Accept={Accept}",
                trimmedDecidedBy,
                roleResult.Value!.Role,
                accept);
            return Result<SuggestedEditDto>.Failure(Error.Forbidden("Only the Owner may accept or reject suggestions."));
        }

        var edit = await dbContext.SuggestedEdits
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (edit is null)
        {
            return Result<SuggestedEditDto>.Failure(Error.NotFound($"SuggestedEdit {id} not found."));
        }

        // Accepting a field proposal whose field has moved can overwrite a newer
        // correction with an older one — a regression that looks like progress. Refuse
        // by default; the Owner may still take it, but has to say so.
        var changedUnderneath = false;
        if (accept && edit.Field is not null)
        {
            var staleness = await DetectFieldChangeAsync(edit, cancellationToken);
            if (!staleness.IsSuccess)
            {
                return Result<SuggestedEditDto>.Failure(staleness.Error!);
            }

            changedUnderneath = staleness.Value;
            if (changedUnderneath && !request.AcceptChangedTarget)
            {
                logger.LogWarning(
                    "Accept refused: {Field} on {TargetType} {TargetId} changed since proposal {EditId} was filed.",
                    edit.Field,
                    edit.TargetType,
                    edit.TargetId,
                    edit.Id);
                return Result<SuggestedEditDto>.Failure(Error.Conflict(
                    $"'{edit.Field}' has changed since this was proposed. Re-read it, then accept again with acceptChangedTarget to take the proposal anyway."));
            }
        }

        var domainError = accept
            ? edit.Accept(trimmedDecidedBy, request.Note, changedUnderneath)
            : edit.Reject(trimmedDecidedBy, request.Note);
        if (domainError is not null)
        {
            return Result<SuggestedEditDto>.Failure(domainError);
        }

        // Write the content before recording the decision, so a rejected value never
        // leaves an accepted proposal behind. If the write fails the decision is not
        // saved either, and the proposal stays pending — the Owner can retry or fall
        // back to opening the entry themselves.
        if (accept && request.Apply && edit.Field is not null)
        {
            var applyError = await ApplyToTargetAsync(edit, cancellationToken);
            if (applyError is not null)
            {
                logger.LogWarning(
                    "Accept-and-apply failed on {TargetType} {TargetId} field {Field}: {Message}",
                    edit.TargetType,
                    edit.TargetId,
                    edit.Field,
                    applyError.Message);
                return Result<SuggestedEditDto>.Failure(applyError);
            }
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
