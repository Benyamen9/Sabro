using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Translations.Domain;
using Sabro.Translations.Infrastructure;

namespace Sabro.Translations.Application.Authors;

internal sealed class AuthorService : IAuthorService
{
    private readonly TranslationsDbContext dbContext;
    private readonly IValidator<CreateAuthorRequest> validator;
    private readonly ILogger<AuthorService> logger;

    public AuthorService(
        TranslationsDbContext dbContext,
        IValidator<CreateAuthorRequest> validator,
        ILogger<AuthorService> logger)
    {
        this.dbContext = dbContext;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<Result<AuthorDto>> CreateAsync(CreateAuthorRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await validator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "Author creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<AuthorDto>.Failure(Error.Validation(fields));
        }

        var domainResult = Author.Create(request.Name, request.SyriacName, request.Title);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "Author creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);

            return Result<AuthorDto>.Failure(domainResult.Error!);
        }

        var author = domainResult.Value!;
        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Author created. Id={AuthorId} Name={AuthorName}", author.Id, author.Name);

        return Result<AuthorDto>.Success(Map(author));
    }

    public async Task<Result<AuthorDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var author = await dbContext.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (author is null)
        {
            return Result<AuthorDto>.Failure(Error.NotFound($"Author {id} not found."));
        }

        return Result<AuthorDto>.Success(Map(author));
    }

    public async Task<Result<PagedResult<AuthorDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<AuthorDto>>.Failure(pageError);
        }

        var query = dbContext.Authors.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuthorDto(a.Id, a.Name, a.SyriacName, a.Title, a.CreatedAt, a.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AuthorDto>>.Success(new PagedResult<AuthorDto>(items, total, page, pageSize));
    }

    private static AuthorDto Map(Author author) => new(
        author.Id,
        author.Name,
        author.SyriacName,
        author.Title,
        author.CreatedAt,
        author.UpdatedAt);
}
