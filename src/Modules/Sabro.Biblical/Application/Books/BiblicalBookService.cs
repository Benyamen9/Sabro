using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Biblical.Domain;
using Sabro.Biblical.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Biblical.Application.Books;

internal sealed class BiblicalBookService : IBiblicalBookService
{
    private readonly BiblicalDbContext dbContext;
    private readonly IValidator<CreateBiblicalBookRequest> validator;
    private readonly ILogger<BiblicalBookService> logger;

    public BiblicalBookService(
        BiblicalDbContext dbContext,
        IValidator<CreateBiblicalBookRequest> validator,
        ILogger<BiblicalBookService> logger)
    {
        this.dbContext = dbContext;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<Result<BiblicalBookDto>> CreateAsync(CreateBiblicalBookRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await validator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            logger.LogWarning(
                "BiblicalBook creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);
            return Result<BiblicalBookDto>.Failure(Error.Validation(fields));
        }

        var domainResult = BiblicalBook.Create(
            request.Code,
            request.EnglishName,
            request.Testament,
            request.Order,
            request.SyriacName);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "BiblicalBook creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<BiblicalBookDto>.Failure(domainResult.Error!);
        }

        var book = domainResult.Value!;
        var existing = await dbContext.Books
            .AsNoTracking()
            .AnyAsync(b => b.Code == book.Code, cancellationToken);
        if (existing)
        {
            return Result<BiblicalBookDto>.Failure(Error.Conflict($"BiblicalBook with Code '{book.Code}' already exists."));
        }

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("BiblicalBook created. Id={BookId} Code={Code}", book.Id, book.Code);
        return Result<BiblicalBookDto>.Success(Map(book));
    }

    public async Task<Result<BiblicalBookDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await dbContext.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return book is null
            ? Result<BiblicalBookDto>.Failure(Error.NotFound($"BiblicalBook {id} not found."))
            : Result<BiblicalBookDto>.Success(Map(book));
    }

    public async Task<Result<BiblicalBookDto>> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length == 0)
        {
            return Result<BiblicalBookDto>.Failure(Error.Validation("Code is required."));
        }

        var book = await dbContext.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == normalized, cancellationToken);
        return book is null
            ? Result<BiblicalBookDto>.Failure(Error.NotFound($"BiblicalBook with code '{normalized}' not found."))
            : Result<BiblicalBookDto>.Success(Map(book));
    }

    public async Task<Result<PagedResult<BiblicalBookDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<BiblicalBookDto>>.Failure(pageError);
        }

        var query = dbContext.Books.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(b => b.Order)
            .ThenBy(b => b.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BiblicalBookDto(b.Id, b.Code, b.EnglishName, b.SyriacName, b.Testament, b.Order, b.CreatedAt, b.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<BiblicalBookDto>>.Success(new PagedResult<BiblicalBookDto>(items, total, page, pageSize));
    }

    private static BiblicalBookDto Map(BiblicalBook book) => new(
        book.Id,
        book.Code,
        book.EnglishName,
        book.SyriacName,
        book.Testament,
        book.Order,
        book.CreatedAt,
        book.UpdatedAt);
}
