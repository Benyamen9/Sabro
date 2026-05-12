using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Reviews.Domain;
using Sabro.Reviews.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Reviews.Application.Approvals;

internal sealed class ApprovalService : IApprovalService
{
    private readonly ReviewsDbContext dbContext;
    private readonly IValidator<CreateApprovalRequest> createValidator;
    private readonly IUserProfileService userProfiles;
    private readonly ILogger<ApprovalService> logger;

    public ApprovalService(
        ReviewsDbContext dbContext,
        IValidator<CreateApprovalRequest> createValidator,
        IUserProfileService userProfiles,
        ILogger<ApprovalService> logger)
    {
        this.dbContext = dbContext;
        this.createValidator = createValidator;
        this.userProfiles = userProfiles;
        this.logger = logger;
    }

    public async Task<Result<ApprovalDto>> CreateAsync(
        CreateApprovalRequest request,
        string decidedByLogtoUserId,
        CancellationToken cancellationToken)
    {
        var trimmedDecidedBy = (decidedByLogtoUserId ?? string.Empty).Trim();
        if (trimmedDecidedBy.Length == 0)
        {
            return Result<ApprovalDto>.Failure(Error.Validation("DecisionByLogtoUserId is required."));
        }

        var shapeResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            logger.LogWarning(
                "Approval creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);
            return Result<ApprovalDto>.Failure(Error.Validation(fields));
        }

        var roleResult = await userProfiles.GetOrCreateForLogtoUserAsync(trimmedDecidedBy, cancellationToken);
        if (!roleResult.IsSuccess)
        {
            return Result<ApprovalDto>.Failure(roleResult.Error!);
        }

        if (roleResult.Value!.Role != Role.Owner)
        {
            logger.LogWarning(
                "Approval creation forbidden. DecidedBy={DecidedBy} ActualRole={Role}",
                trimmedDecidedBy,
                roleResult.Value.Role);
            return Result<ApprovalDto>.Failure(Error.Forbidden("Only the Owner may create approvals."));
        }

        var domainResult = request.TargetType switch
        {
            ApprovalTargetType.Segment => Approval.CreateSegment(
                request.SourceId,
                request.ChapterNumber,
                request.VerseNumber!.Value,
                request.Version!.Value,
                request.Status,
                trimmedDecidedBy,
                request.Note),
            ApprovalTargetType.Chapter => Approval.CreateChapter(
                request.SourceId,
                request.ChapterNumber,
                request.Status,
                trimmedDecidedBy,
                request.Note),
            _ => Result<Approval>.Failure(Error.Validation("TargetType is invalid.")),
        };

        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "Approval creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<ApprovalDto>.Failure(domainResult.Error!);
        }

        var approval = domainResult.Value!;
        dbContext.Approvals.Add(approval);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Approval created. Id={ApprovalId} TargetType={TargetType} SourceId={SourceId} Chapter={Chapter} Verse={Verse} Status={Status}",
            approval.Id,
            approval.TargetType,
            approval.SourceId,
            approval.ChapterNumber,
            approval.VerseNumber,
            approval.Status);
        return Result<ApprovalDto>.Success(Map(approval));
    }

    public async Task<Result<ApprovalDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var approval = await dbContext.Approvals
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return approval is null
            ? Result<ApprovalDto>.Failure(Error.NotFound($"Approval {id} not found."))
            : Result<ApprovalDto>.Success(Map(approval));
    }

    public async Task<Result<PagedResult<ApprovalDto>>> ListAsync(
        ApprovalListFilters filters,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<ApprovalDto>>.Failure(pageError);
        }

        var query = dbContext.Approvals.AsNoTracking();

        if (filters.TargetType is not null)
        {
            var targetType = filters.TargetType.Value;
            query = query.Where(a => a.TargetType == targetType);
        }

        if (filters.Status is not null)
        {
            var status = filters.Status.Value;
            query = query.Where(a => a.Status == status);
        }

        if (filters.SourceId is not null)
        {
            var sourceId = filters.SourceId.Value;
            query = query.Where(a => a.SourceId == sourceId);
        }

        if (filters.ChapterNumber is not null)
        {
            var chapterNumber = filters.ChapterNumber.Value;
            query = query.Where(a => a.ChapterNumber == chapterNumber);
        }

        if (filters.VerseNumber is not null)
        {
            var verseNumber = filters.VerseNumber.Value;
            query = query.Where(a => a.VerseNumber == verseNumber);
        }

        var trimmedDecidedBy = string.IsNullOrWhiteSpace(filters.DecisionByLogtoUserId)
            ? null
            : filters.DecisionByLogtoUserId.Trim();
        if (trimmedDecidedBy is not null)
        {
            query = query.Where(a => a.DecisionByLogtoUserId == trimmedDecidedBy);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.DecisionAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var mapped = items.Select(Map).ToArray();
        return Result<PagedResult<ApprovalDto>>.Success(
            new PagedResult<ApprovalDto>(mapped, total, page, pageSize));
    }

    public async Task<Result<EffectiveChapterApprovalsDto>> GetEffectiveForChapterAsync(
        Guid sourceId,
        int chapterNumber,
        CancellationToken cancellationToken)
    {
        if (sourceId == Guid.Empty)
        {
            return Result<EffectiveChapterApprovalsDto>.Failure(Error.Validation("SourceId is required."));
        }

        if (chapterNumber < 1)
        {
            return Result<EffectiveChapterApprovalsDto>.Failure(Error.Validation("ChapterNumber must be 1 or greater."));
        }

        var chapterApproval = await dbContext.Approvals
            .AsNoTracking()
            .Where(a => a.SourceId == sourceId
                && a.ChapterNumber == chapterNumber
                && a.TargetType == ApprovalTargetType.Chapter)
            .OrderByDescending(a => a.DecisionAt)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var verseRows = await dbContext.Approvals
            .AsNoTracking()
            .Where(a => a.SourceId == sourceId
                && a.ChapterNumber == chapterNumber
                && a.TargetType == ApprovalTargetType.Segment)
            .OrderByDescending(a => a.DecisionAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);

        var latestPerVerse = verseRows
            .GroupBy(a => a.VerseNumber)
            .Select(g => g.First())
            .Select(Map)
            .ToList();

        return Result<EffectiveChapterApprovalsDto>.Success(new EffectiveChapterApprovalsDto(
            sourceId,
            chapterNumber,
            chapterApproval is null ? null : Map(chapterApproval),
            latestPerVerse));
    }

    private static ApprovalDto Map(Approval approval) => new(
        approval.Id,
        approval.TargetType,
        approval.SourceId,
        approval.ChapterNumber,
        approval.VerseNumber,
        approval.Version,
        approval.Status,
        approval.DecisionByLogtoUserId,
        approval.DecisionAt,
        approval.Note,
        approval.CreatedAt,
        approval.UpdatedAt);
}
