using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Lexicon.Application.Entries;

internal sealed class LexiconEntryService : ILexiconEntryService
{
    private readonly LexiconDbContext dbContext;
    private readonly IValidator<CreateLexiconEntryRequest> validator;
    private readonly ILogger<LexiconEntryService> logger;

    public LexiconEntryService(
        LexiconDbContext dbContext,
        IValidator<CreateLexiconEntryRequest> validator,
        ILogger<LexiconEntryService> logger)
    {
        this.dbContext = dbContext;
        this.validator = validator;
        this.logger = logger;
    }

    public async Task<Result<LexiconEntryDto>> CreateAsync(CreateLexiconEntryRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await validator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "LexiconEntry creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<LexiconEntryDto>.Failure(Error.Validation(fields));
        }

        var meanings = new List<LexiconMeaning>();
        if (request.Meanings is not null)
        {
            foreach (var meaningRequest in request.Meanings)
            {
                var meaningResult = LexiconMeaning.Create(meaningRequest.Language, meaningRequest.Text);
                if (!meaningResult.IsSuccess)
                {
                    logger.LogWarning(
                        "LexiconEntry meaning rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                        meaningResult.Error!.Code,
                        meaningResult.Error.Message);
                    return Result<LexiconEntryDto>.Failure(meaningResult.Error!);
                }

                meanings.Add(meaningResult.Value!);
            }
        }

        var domainResult = LexiconEntry.Create(
            request.SyriacUnvocalized,
            request.SblTransliteration,
            request.GrammaticalCategory,
            request.SyriacVocalized,
            request.RootId,
            request.TransliterationVariants,
            request.Morphology,
            meanings);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "LexiconEntry creation rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);

            return Result<LexiconEntryDto>.Failure(domainResult.Error!);
        }

        var entry = domainResult.Value!;
        dbContext.Entries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "LexiconEntry created. Id={EntryId} SyriacUnvocalized={Syriac} Category={Category}",
            entry.Id,
            entry.SyriacUnvocalized,
            entry.GrammaticalCategory);

        return Result<LexiconEntryDto>.Success(Map(entry));
    }

    public async Task<Result<LexiconEntryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return Result<LexiconEntryDto>.Failure(Error.NotFound($"LexiconEntry {id} not found."));
        }

        return Result<LexiconEntryDto>.Success(Map(entry));
    }

    public async Task<Result<PagedResult<LexiconEntryDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<LexiconEntryDto>>.Failure(pageError);
        }

        var query = dbContext.Entries.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<LexiconEntryDto>>.Success(
            new PagedResult<LexiconEntryDto>(items.Select(Map).ToArray(), total, page, pageSize));
    }

    private static LexiconEntryDto Map(LexiconEntry entry) => new(
        entry.Id,
        entry.SyriacUnvocalized,
        entry.SyriacVocalized,
        entry.RootId,
        entry.SblTransliteration,
        entry.TransliterationVariants.ToArray(),
        entry.GrammaticalCategory,
        entry.Morphology,
        entry.Meanings.Select(m => new LexiconMeaningDto(m.Language, m.Text)).ToArray(),
        entry.CreatedAt,
        entry.UpdatedAt);
}
