using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Roots;

internal sealed class LexiconRootService : ILexiconRootService
{
    private readonly LexiconDbContext dbContext;
    private readonly IValidator<CreateLexiconRootRequest> validator;
    private readonly ILogger<LexiconRootService> logger;

    public LexiconRootService(
        LexiconDbContext dbContext,
        IValidator<CreateLexiconRootRequest> validator,
        ILogger<LexiconRootService> logger)
    {
        this.dbContext = dbContext;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<Result<LexiconRootDto>> CreateAsync(CreateLexiconRootRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await validator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "LexiconRoot creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<LexiconRootDto>.Failure(Error.Validation(fields));
        }

        var domainResult = LexiconRoot.Create(request.Form);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "LexiconRoot creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);

            return Result<LexiconRootDto>.Failure(domainResult.Error!);
        }

        var root = domainResult.Value!;
        dbContext.Roots.Add(root);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("LexiconRoot created. Id={RootId} Form={RootForm}", root.Id, root.Form);

        return Result<LexiconRootDto>.Success(Map(root));
    }

    public async Task<Result<LexiconRootDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var root = await dbContext.Roots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (root is null)
        {
            return Result<LexiconRootDto>.Failure(Error.NotFound($"LexiconRoot {id} not found."));
        }

        return Result<LexiconRootDto>.Success(Map(root));
    }

    public async Task<Result<PagedResult<LexiconRootDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<LexiconRootDto>>.Failure(pageError);
        }

        var query = dbContext.Roots.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new LexiconRootDto(r.Id, r.Form, r.CreatedAt, r.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<LexiconRootDto>>.Success(new PagedResult<LexiconRootDto>(items, total, page, pageSize));
    }

    private static LexiconRootDto Map(LexiconRoot root) => new(
        root.Id,
        root.Form,
        root.CreatedAt,
        root.UpdatedAt);
}
