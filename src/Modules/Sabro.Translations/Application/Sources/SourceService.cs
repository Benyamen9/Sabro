using FluentValidation;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Results;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Sources;

internal sealed class SourceService : ISourceService
{
    private readonly TranslationsDbContext dbContext;
    private readonly IValidator<CreateSourceRequest> validator;
    private readonly ILogger<SourceService> logger;

    public SourceService(
        TranslationsDbContext dbContext,
        IValidator<CreateSourceRequest> validator,
        ILogger<SourceService> logger)
    {
        this.dbContext = dbContext;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<Result<SourceDto>> CreateAsync(CreateSourceRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await validator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "Source creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<SourceDto>.Failure(Error.Validation(fields));
        }

        var domainResult = Source.Create(
            request.AuthorId,
            request.Title,
            request.OriginalLanguageCode,
            request.Description);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "Source creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);

            return Result<SourceDto>.Failure(domainResult.Error!);
        }

        var source = domainResult.Value!;
        dbContext.Sources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Source created. Id={SourceId} AuthorId={AuthorId} Title={SourceTitle}",
            source.Id,
            source.AuthorId,
            source.Title);

        return Result<SourceDto>.Success(new SourceDto(
            source.Id,
            source.AuthorId,
            source.Title,
            source.OriginalLanguageCode,
            source.Description,
            source.CreatedAt,
            source.UpdatedAt));
    }
}
