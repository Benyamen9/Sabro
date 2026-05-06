using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Annotations;

internal sealed class AnnotationService : IAnnotationService
{
    private readonly TranslationsDbContext dbContext;
    private readonly IValidator<CreateAnnotationRequest> createValidator;
    private readonly IValidator<EditAnnotationRequest> editValidator;
    private readonly ILogger<AnnotationService> logger;

    public AnnotationService(
        TranslationsDbContext dbContext,
        IValidator<CreateAnnotationRequest> createValidator,
        IValidator<EditAnnotationRequest> editValidator,
        ILogger<AnnotationService> logger)
    {
        this.dbContext = dbContext;
        this.createValidator = createValidator;
        this.editValidator = editValidator;
        this.logger = logger;
    }

    public async Task<Result<AnnotationDto>> CreateAsync(CreateAnnotationRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "Annotation creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<AnnotationDto>.Failure(Error.Validation(fields));
        }

        var domainResult = Annotation.Create(
            request.SegmentId,
            request.AnchorStart,
            request.AnchorEnd,
            request.Body);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "Annotation creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);

            return Result<AnnotationDto>.Failure(domainResult.Error!);
        }

        var annotation = domainResult.Value!;
        dbContext.Annotations.Add(annotation);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Annotation created. Id={AnnotationId} SegmentId={SegmentId} Version={Version}",
            annotation.Id,
            annotation.SegmentId,
            annotation.Version);

        return Result<AnnotationDto>.Success(Map(annotation));
    }

    public async Task<Result<AnnotationDto>> EditAsync(EditAnnotationRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await editValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "Annotation edit rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<AnnotationDto>.Failure(Error.Validation(fields));
        }

        var existing = await dbContext.Annotations
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AnnotationId, cancellationToken);
        if (existing is null)
        {
            logger.LogWarning("Annotation edit rejected: annotation not found. Id={AnnotationId}", request.AnnotationId);
            return Result<AnnotationDto>.Failure(Error.NotFound($"Annotation {request.AnnotationId} not found."));
        }

        var nextResult = existing.CreateNextVersion(request.NewBody);
        if (!nextResult.IsSuccess)
        {
            logger.LogWarning(
                "Annotation edit rejected by domain invariant. Id={AnnotationId} Code={ErrorCode} Message={ErrorMessage}",
                request.AnnotationId,
                nextResult.Error!.Code,
                nextResult.Error.Message);

            return Result<AnnotationDto>.Failure(nextResult.Error!);
        }

        var next = nextResult.Value!;
        dbContext.Annotations.Add(next);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Annotation edited. PreviousId={PreviousId} NewId={NewId} Version={Version}",
            existing.Id,
            next.Id,
            next.Version);

        return Result<AnnotationDto>.Success(Map(next));
    }

    public async Task<Result<AnnotationDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var annotation = await dbContext.Annotations
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (annotation is null)
        {
            return Result<AnnotationDto>.Failure(Error.NotFound($"Annotation {id} not found."));
        }

        return Result<AnnotationDto>.Success(Map(annotation));
    }

    public async Task<Result<PagedResult<AnnotationDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<AnnotationDto>>.Failure(pageError);
        }

        var query = dbContext.Annotations.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AnnotationDto(
                a.Id,
                a.SegmentId,
                a.AnchorStart,
                a.AnchorEnd,
                a.Body,
                a.Version,
                a.PreviousVersionId,
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AnnotationDto>>.Success(new PagedResult<AnnotationDto>(items, total, page, pageSize));
    }

    private static AnnotationDto Map(Annotation annotation) => new(
        annotation.Id,
        annotation.SegmentId,
        annotation.AnchorStart,
        annotation.AnchorEnd,
        annotation.Body,
        annotation.Version,
        annotation.PreviousVersionId,
        annotation.CreatedAt,
        annotation.UpdatedAt);
}
