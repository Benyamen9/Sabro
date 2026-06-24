using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltho;

internal sealed class MelthoLibraryService : IMelthoLibraryService
{
    private readonly PlayDbContext dbContext;
    private readonly ILexiconLibraryReader libraryReader;
    private readonly TimeProvider timeProvider;

    public MelthoLibraryService(
        PlayDbContext dbContext,
        ILexiconLibraryReader libraryReader,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.libraryReader = libraryReader;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<PagedResult<MelthoLibraryEntryDto>>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var validationError = PageRequest.Validate(page, pageSize);
        if (validationError is not null)
        {
            return Result<PagedResult<MelthoLibraryEntryDto>>.Failure(validationError);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var pastPuzzles = dbContext.MelthoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Meltho && p.Date < today);

        var total = await pastPuzzles
            .Select(p => p.LexiconEntryId)
            .Distinct()
            .CountAsync(cancellationToken);

        // One row per word, ordered by the most recent day it was served.
        var pageRows = await pastPuzzles
            .GroupBy(p => p.LexiconEntryId)
            .Select(g => new { LexiconEntryId = g.Key, LastPlayedOn = g.Max(p => p.Date) })
            .OrderByDescending(x => x.LastPlayedOn)
            .ThenByDescending(x => x.LexiconEntryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var ids = pageRows.Select(r => r.LexiconEntryId).ToList();
        var items = await libraryReader.GetLibraryListAsync(ids, cancellationToken);
        var byId = items.ToDictionary(i => i.Id);

        var dtos = new List<MelthoLibraryEntryDto>(pageRows.Count);
        foreach (var row in pageRows)
        {
            // A word whose entry was hard-deleted is simply skipped (it has no projection).
            if (byId.TryGetValue(row.LexiconEntryId, out var item))
            {
                dtos.Add(new MelthoLibraryEntryDto(
                    row.LastPlayedOn,
                    item.Id,
                    item.SyriacUnvocalized,
                    item.Meanings.Select(m => new MelthoPuzzleMeaningDto(m.Language, m.Text)).ToArray()));
            }
        }

        return Result<PagedResult<MelthoLibraryEntryDto>>.Success(
            new PagedResult<MelthoLibraryEntryDto>(dtos, total, page, pageSize));
    }

    public async Task<Result<MelthoLibraryDetailDto>> GetDetailAsync(Guid lexiconEntryId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var playedOn = await dbContext.MelthoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Meltho && p.LexiconEntryId == lexiconEntryId && p.Date < today)
            .Select(p => p.Date)
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken);
        if (playedOn.Count == 0)
        {
            return Result<MelthoLibraryDetailDto>.Failure(
                Error.NotFound("This word is not in the Meltho library yet."));
        }

        var detail = await libraryReader.GetLibraryDetailAsync(lexiconEntryId, cancellationToken);
        if (detail is null)
        {
            return Result<MelthoLibraryDetailDto>.Failure(
                Error.NotFound("This word could not be resolved."));
        }

        var dto = new MelthoLibraryDetailDto(
            detail.Id,
            detail.SyriacUnvocalized,
            detail.SyriacVocalized,
            detail.SblTransliteration,
            detail.TransliterationVariants,
            detail.GrammaticalCategory,
            detail.Morphology,
            detail.PlayableLength,
            detail.Meanings.Select(m => new MelthoPuzzleMeaningDto(m.Language, m.Text)).ToArray(),
            detail.Composition,
            playedOn);

        return Result<MelthoLibraryDetailDto>.Success(dto);
    }
}
