using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.Lexicon.Application.Entries;

internal sealed class LexiconEntryService : ILexiconEntryService
{
    private readonly LexiconDbContext dbContext;
    private readonly IValidator<CreateLexiconEntryRequest> createValidator;
    private readonly IValidator<UpdateLexiconEntryRequest> updateValidator;
    private readonly ISearchIndex<LexiconEntrySearchDocument> searchIndex;
    private readonly ILogger<LexiconEntryService> logger;

    public LexiconEntryService(
        LexiconDbContext dbContext,
        IValidator<CreateLexiconEntryRequest> createValidator,
        IValidator<UpdateLexiconEntryRequest> updateValidator,
        ISearchIndex<LexiconEntrySearchDocument> searchIndex,
        ILogger<LexiconEntryService> logger)
    {
        this.dbContext = dbContext;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
        this.searchIndex = searchIndex;
        this.logger = logger;
    }

    public async Task<Result<LexiconEntryDto>> CreateAsync(CreateLexiconEntryRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "LexiconEntry creation rejected at request validation. Fields={FieldNames}",
                fields.Keys);

            return Result<LexiconEntryDto>.Failure(Error.Validation(fields));
        }

        var meaningsResult = BuildMeanings(request.Meanings);
        if (!meaningsResult.IsSuccess)
        {
            return Result<LexiconEntryDto>.Failure(meaningsResult.Error!);
        }

        var domainResult = LexiconEntry.Create(
            request.SyriacUnvocalized,
            request.SblTransliteration,
            request.GrammaticalCategory,
            request.SyriacVocalized,
            request.RootId,
            request.TransliterationVariants,
            request.Morphology,
            meaningsResult.Value!);
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

        await ReindexAsync(entry, cancellationToken);

        return Result<LexiconEntryDto>.Success(Map(entry));
    }

    public async Task<Result<LexiconEntryDto>> UpdateAsync(Guid id, UpdateLexiconEntryRequest request, CancellationToken cancellationToken)
    {
        var shapeResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);

            logger.LogWarning(
                "LexiconEntry update rejected at request validation. Id={EntryId} Fields={FieldNames}",
                id,
                fields.Keys);

            return Result<LexiconEntryDto>.Failure(Error.Validation(fields));
        }

        var entry = await dbContext.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return Result<LexiconEntryDto>.Failure(Error.NotFound($"LexiconEntry {id} not found."));
        }

        var meaningsResult = BuildMeanings(request.Meanings);
        if (!meaningsResult.IsSuccess)
        {
            return Result<LexiconEntryDto>.Failure(meaningsResult.Error!);
        }

        var error = entry.Update(
            request.SyriacUnvocalized,
            request.SblTransliteration,
            request.GrammaticalCategory,
            request.SyriacVocalized,
            request.RootId,
            request.TransliterationVariants,
            request.Morphology,
            meaningsResult.Value!);
        if (error is not null)
        {
            logger.LogWarning(
                "LexiconEntry update rejected by domain invariant. Id={EntryId} Code={ErrorCode} Message={ErrorMessage}",
                id,
                error.Code,
                error.Message);

            return Result<LexiconEntryDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("LexiconEntry updated. Id={EntryId}", entry.Id);

        await ReindexAsync(entry, cancellationToken);

        return Result<LexiconEntryDto>.Success(Map(entry));
    }

    public async Task<Error?> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return Error.NotFound($"LexiconEntry {id} not found.");
        }

        dbContext.Entries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("LexiconEntry deleted. Id={EntryId}", id);

        await searchIndex.DeleteAsync(id.ToString("D"), cancellationToken);

        return null;
    }

    public Task<Result<LexiconEntryDto>> PublishAsync(Guid id, CancellationToken cancellationToken) =>
        MutateAsync(id, entry => entry.Publish(), "published", cancellationToken);

    public Task<Result<LexiconEntryDto>> UnpublishAsync(Guid id, CancellationToken cancellationToken) =>
        MutateAsync(
            id,
            entry =>
            {
                entry.ReturnToDraft();
                return null;
            },
            "returned to draft",
            cancellationToken);

    public Task<Result<LexiconEntryDto>> SetPlayableAsync(Guid id, bool playable, CancellationToken cancellationToken) =>
        MutateAsync(id, entry => entry.SetPlayable(playable), $"playable={playable}", cancellationToken);

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

    public async Task<Result<LexiconEntryDto>> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.Status == LexiconEntryStatus.Published, cancellationToken);
        if (entry is null)
        {
            return Result<LexiconEntryDto>.Failure(Error.NotFound($"LexiconEntry {id} not found."));
        }

        return Result<LexiconEntryDto>.Success(Map(entry));
    }

    public Task<Result<PagedResult<LexiconEntryDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        ListCoreAsync(page, pageSize, publishedOnly: false, cancellationToken);

    public Task<Result<PagedResult<LexiconEntryDto>>> ListPublishedAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        ListCoreAsync(page, pageSize, publishedOnly: true, cancellationToken);

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
        entry.Status,
        entry.PlayableInMeltho,
        entry.PlayableLength,
        entry.CreatedAt,
        entry.UpdatedAt);

    private Result<List<LexiconMeaning>> BuildMeanings(IReadOnlyList<CreateLexiconMeaningRequest>? requests)
    {
        var meanings = new List<LexiconMeaning>();
        if (requests is null)
        {
            return Result<List<LexiconMeaning>>.Success(meanings);
        }

        foreach (var meaningRequest in requests)
        {
            var meaningResult = LexiconMeaning.Create(meaningRequest.Language, meaningRequest.Text);
            if (!meaningResult.IsSuccess)
            {
                logger.LogWarning(
                    "LexiconEntry meaning rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                    meaningResult.Error!.Code,
                    meaningResult.Error.Message);
                return Result<List<LexiconMeaning>>.Failure(meaningResult.Error!);
            }

            meanings.Add(meaningResult.Value!);
        }

        return Result<List<LexiconMeaning>>.Success(meanings);
    }

    private async Task<Result<LexiconEntryDto>> MutateAsync(
        Guid id,
        Func<LexiconEntry, Error?> mutate,
        string action,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.Entries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return Result<LexiconEntryDto>.Failure(Error.NotFound($"LexiconEntry {id} not found."));
        }

        var error = mutate(entry);
        if (error is not null)
        {
            logger.LogWarning(
                "LexiconEntry state change rejected. Id={EntryId} Action={Action} Code={ErrorCode} Message={ErrorMessage}",
                id,
                action,
                error.Code,
                error.Message);

            return Result<LexiconEntryDto>.Failure(error);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("LexiconEntry state changed. Id={EntryId} Action={Action}", id, action);

        await ReindexAsync(entry, cancellationToken);

        return Result<LexiconEntryDto>.Success(Map(entry));
    }

    private async Task<Result<PagedResult<LexiconEntryDto>>> ListCoreAsync(int page, int pageSize, bool publishedOnly, CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<LexiconEntryDto>>.Failure(pageError);
        }

        var query = dbContext.Entries.AsNoTracking();
        if (publishedOnly)
        {
            query = query.Where(e => e.Status == LexiconEntryStatus.Published);
        }

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

    private async Task ReindexAsync(LexiconEntry entry, CancellationToken cancellationToken)
    {
        var rootForm = await ResolveRootFormAsync(entry.RootId, cancellationToken);
        await searchIndex.UpsertAsync(LexiconEntryDocumentMapper.Map(entry, rootForm), cancellationToken);
    }

    private async Task<string?> ResolveRootFormAsync(Guid? rootId, CancellationToken cancellationToken)
    {
        if (!rootId.HasValue)
        {
            return null;
        }

        return await dbContext.Roots
            .AsNoTracking()
            .Where(r => r.Id == rootId.Value)
            .Select(r => r.Form)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
