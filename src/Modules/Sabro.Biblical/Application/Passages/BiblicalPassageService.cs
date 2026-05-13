using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;
using Sabro.Biblical.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.Biblical.Application.Passages;

internal sealed class BiblicalPassageService : IBiblicalPassageService
{
    private readonly BiblicalDbContext dbContext;
    private readonly IValidator<GetOrCreateBiblicalPassageRequest> validator;
    private readonly ISearchIndex<BiblicalPassageSearchDocument> searchIndex;
    private readonly ILogger<BiblicalPassageService> logger;

    public BiblicalPassageService(
        BiblicalDbContext dbContext,
        IValidator<GetOrCreateBiblicalPassageRequest> validator,
        ISearchIndex<BiblicalPassageSearchDocument> searchIndex,
        ILogger<BiblicalPassageService> logger)
    {
        this.dbContext = dbContext;
        this.validator = validator;
        this.searchIndex = searchIndex;
        this.logger = logger;
    }

    public async Task<Result<BiblicalPassageLookupResult>> GetOrCreateAsync(GetOrCreateBiblicalPassageRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await validator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            logger.LogWarning(
                "BiblicalPassage get-or-create rejected at request validation. Fields={FieldNames}",
                fields.Keys);
            return Result<BiblicalPassageLookupResult>.Failure(Error.Validation(fields));
        }

        var normalizedCode = request.BookCode.Trim().ToUpperInvariant();
        var book = await dbContext.Books
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code == normalizedCode, cancellationToken);
        if (book is null)
        {
            return Result<BiblicalPassageLookupResult>.Failure(
                Error.NotFound($"BiblicalBook with code '{normalizedCode}' not found."));
        }

        var existing = await dbContext.Passages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.BookId == book.Id && p.ChapterNumber == request.ChapterNumber && p.VerseNumber == request.VerseNumber,
                cancellationToken);
        if (existing is not null)
        {
            return Result<BiblicalPassageLookupResult>.Success(new BiblicalPassageLookupResult(Map(existing, book.Code), WasCreated: false));
        }

        var domainResult = BiblicalPassage.Create(book.Id, request.ChapterNumber, request.VerseNumber);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "BiblicalPassage creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<BiblicalPassageLookupResult>.Failure(domainResult.Error!);
        }

        var passage = domainResult.Value!;
        dbContext.Passages.Add(passage);
        await dbContext.SaveChangesAsync(cancellationToken);

        await searchIndex.UpsertAsync(BiblicalPassageDocumentMapper.Map(passage, book), cancellationToken);

        logger.LogInformation(
            "BiblicalPassage created. Id={PassageId} BookCode={BookCode} Chapter={Chapter} Verse={Verse}",
            passage.Id,
            book.Code,
            passage.ChapterNumber,
            passage.VerseNumber);

        return Result<BiblicalPassageLookupResult>.Success(new BiblicalPassageLookupResult(Map(passage, book.Code), WasCreated: true));
    }

    public async Task<Result<BiblicalPassageDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await (from p in dbContext.Passages.AsNoTracking()
                         join b in dbContext.Books.AsNoTracking() on p.BookId equals b.Id
                         where p.Id == id
                         select new { Passage = p, BookCode = b.Code })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? Result<BiblicalPassageDto>.Failure(Error.NotFound($"BiblicalPassage {id} not found."))
            : Result<BiblicalPassageDto>.Success(Map(row.Passage, row.BookCode));
    }

    public async Task<Result<PagedResult<BiblicalPassageDto>>> ListAsync(string? bookCode, int? chapterNumber, int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<BiblicalPassageDto>>.Failure(pageError);
        }

        var normalizedCode = string.IsNullOrWhiteSpace(bookCode) ? null : bookCode.Trim().ToUpperInvariant();
        var query = from p in dbContext.Passages.AsNoTracking()
                    join b in dbContext.Books.AsNoTracking() on p.BookId equals b.Id
                    select new { Passage = p, BookCode = b.Code };

        if (normalizedCode is not null)
        {
            query = query.Where(x => x.BookCode == normalizedCode);
        }

        if (chapterNumber is not null)
        {
            query = query.Where(x => x.Passage.ChapterNumber == chapterNumber.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.BookCode)
            .ThenBy(x => x.Passage.ChapterNumber)
            .ThenBy(x => x.Passage.VerseNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => Map(r.Passage, r.BookCode)).ToArray();
        return Result<PagedResult<BiblicalPassageDto>>.Success(new PagedResult<BiblicalPassageDto>(items, total, page, pageSize));
    }

    private static BiblicalPassageDto Map(BiblicalPassage passage, string bookCode) => new(
        passage.Id,
        passage.BookId,
        bookCode,
        passage.ChapterNumber,
        passage.VerseNumber,
        passage.CreatedAt,
        passage.UpdatedAt);
}
