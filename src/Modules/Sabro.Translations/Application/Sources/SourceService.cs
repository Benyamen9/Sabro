using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Pagination;
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

        return Result<SourceDto>.Success(Map(source));
    }

    public async Task<Result<SourceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await dbContext.Sources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (source is null)
        {
            return Result<SourceDto>.Failure(Error.NotFound($"Source {id} not found."));
        }

        return Result<SourceDto>.Success(Map(source));
    }

    public async Task<Result<PagedResult<SourceDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<SourceDto>>.Failure(pageError);
        }

        var query = dbContext.Sources.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SourceDto(
                s.Id,
                s.AuthorId,
                s.Title,
                s.OriginalLanguageCode,
                s.Description,
                s.CreatedAt,
                s.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<SourceDto>>.Success(new PagedResult<SourceDto>(items, total, page, pageSize));
    }

    private static SourceDto Map(Source source) => new(
        source.Id,
        source.AuthorId,
        source.Title,
        source.OriginalLanguageCode,
        source.Description,
        source.CreatedAt,
        source.UpdatedAt);
}
