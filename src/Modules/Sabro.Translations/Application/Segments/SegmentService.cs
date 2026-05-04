using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Results;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Segments;

internal sealed class SegmentService : ISegmentService
{
    private readonly TranslationsDbContext dbContext;
    private readonly IValidator<CreateSegmentRequest> createValidator;
    private readonly IValidator<EditSegmentRequest> editValidator;
    private readonly ILogger<SegmentService> logger;

    public SegmentService(
        TranslationsDbContext dbContext,
        IValidator<CreateSegmentRequest> createValidator,
        IValidator<EditSegmentRequest> editValidator,
        ILogger<SegmentService> logger)
    {
        this.dbContext = dbContext;
        this.createValidator = createValidator;
        this.editValidator = editValidator;
        this.logger = logger;
    }

    public async Task<Result<SegmentDto>> CreateAsync(CreateSegmentRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "Segment creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<SegmentDto>.Failure(Error.Validation(fields));
        }

        var domainResult = Segment.Create(
            request.SourceId,
            request.ChapterNumber,
            request.VerseNumber,
            request.TextVersionId,
            request.Content);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "Segment creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);

            return Result<SegmentDto>.Failure(domainResult.Error!);
        }

        var segment = domainResult.Value!;
        dbContext.Segments.Add(segment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Segment created. Id={SegmentId} SourceId={SourceId} Chapter={Chapter} Verse={Verse} Version={Version}",
            segment.Id,
            segment.SourceId,
            segment.ChapterNumber,
            segment.VerseNumber,
            segment.Version);

        return Map(segment);
    }

    public async Task<Result<SegmentDto>> EditAsync(EditSegmentRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await editValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "Segment edit rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<SegmentDto>.Failure(Error.Validation(fields));
        }

        var existing = await dbContext.Segments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SegmentId, cancellationToken);
        if (existing is null)
        {
            logger.LogWarning("Segment edit rejected: segment not found. Id={SegmentId}", request.SegmentId);
            return Result<SegmentDto>.Failure(Error.NotFound($"Segment {request.SegmentId} not found."));
        }

        var nextResult = existing.CreateNextVersion(request.NewContent);
        if (!nextResult.IsSuccess)
        {
            logger.LogWarning(
                "Segment edit rejected by domain invariant. Id={SegmentId} Code={ErrorCode} Message={ErrorMessage}",
                request.SegmentId,
                nextResult.Error!.Code,
                nextResult.Error.Message);

            return Result<SegmentDto>.Failure(nextResult.Error!);
        }

        var next = nextResult.Value!;
        dbContext.Segments.Add(next);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Segment edited. PreviousId={PreviousId} NewId={NewId} Version={Version}",
            existing.Id,
            next.Id,
            next.Version);

        return Map(next);
    }

    private static Result<SegmentDto> Map(Segment segment) =>
        Result<SegmentDto>.Success(new SegmentDto(
            segment.Id,
            segment.SourceId,
            segment.ChapterNumber,
            segment.VerseNumber,
            segment.TextVersionId,
            segment.Content,
            segment.Version,
            segment.PreviousVersionId,
            segment.CreatedAt,
            segment.UpdatedAt));
}
