using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltho;

internal sealed class MelthoPuzzleService : IMelthoPuzzleService
{
    /// <summary>Hard server-side bound on playable length, re-enforced here so a mis-flagged out-of-range entry can never be served.</summary>
    private const int MinPlayableLength = 2;
    private const int MaxPlayableLength = 8;

    private readonly PlayDbContext dbContext;
    private readonly ILexiconPlayablePool playablePool;
    private readonly MelthoOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<MelthoPuzzleService> logger;

    public MelthoPuzzleService(
        PlayDbContext dbContext,
        ILexiconPlayablePool playablePool,
        IOptions<MelthoOptions> options,
        TimeProvider timeProvider,
        ILogger<MelthoPuzzleService> logger)
    {
        this.dbContext = dbContext;
        this.playablePool = playablePool;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<MelthoPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var existing = await dbContext.MelthoDailyPuzzles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == Games.Meltho && p.Date == today, cancellationToken);
        if (existing is not null)
        {
            return await RenderAsync(today, existing.LexiconEntryId, cancellationToken);
        }

        var selection = await SelectEntryIdAsync(today, cancellationToken);
        if (!selection.IsSuccess)
        {
            return Result<MelthoPuzzleDto>.Failure(selection.Error!);
        }

        var puzzleResult = MelthoDailyPuzzle.Create(Games.Meltho, today, selection.Value);
        if (!puzzleResult.IsSuccess)
        {
            return Result<MelthoPuzzleDto>.Failure(puzzleResult.Error!);
        }

        var puzzle = puzzleResult.Value!;
        dbContext.MelthoDailyPuzzles.Add(puzzle);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race on the unique (game, date) key — another request created
            // today's puzzle first. Re-read and serve their word so every player
            // sees the same one.
            dbContext.Entry(puzzle).State = EntityState.Detached;
            var raced = await dbContext.MelthoDailyPuzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == Games.Meltho && p.Date == today, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return await RenderAsync(today, raced.LexiconEntryId, cancellationToken);
        }

        logger.LogInformation(
            "Meltho daily puzzle selected. Date={Date} LexiconEntryId={LexiconEntryId}",
            today,
            puzzle.LexiconEntryId);

        return await RenderAsync(today, puzzle.LexiconEntryId, cancellationToken);
    }

    private async Task<Result<Guid>> SelectEntryIdAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var windowDays = Math.Max(0, options.AntiRepetitionWindowDays);
        var cutoff = today.AddDays(-windowDays);

        var recentlyServed = await dbContext.MelthoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Meltho && p.Date > cutoff)
            .Select(p => p.LexiconEntryId)
            .ToListAsync(cancellationToken);
        var excluded = recentlyServed.ToHashSet();

        var eligible = await playablePool.GetEligibleEntryIdsAsync(MinPlayableLength, MaxPlayableLength, cancellationToken);
        var candidates = eligible.Where(id => !excluded.Contains(id)).ToList();
        if (candidates.Count == 0)
        {
            logger.LogWarning(
                "Meltho daily puzzle selection found no eligible word. EligibleCount={EligibleCount} ExcludedCount={ExcludedCount} WindowDays={WindowDays}",
                eligible.Count,
                excluded.Count,
                windowDays);
            return Result<Guid>.Failure(Error.Conflict(
                "No eligible Meltho word is available for today. The playable pool may be too small for the anti-repetition window."));
        }

        var picked = candidates[Random.Shared.Next(candidates.Count)];
        return Result<Guid>.Success(picked);
    }

    private async Task<Result<MelthoPuzzleDto>> RenderAsync(DateOnly date, Guid lexiconEntryId, CancellationToken cancellationToken)
    {
        var entry = await playablePool.GetPlayableEntryAsync(lexiconEntryId, cancellationToken);
        if (entry is null)
        {
            logger.LogError(
                "Meltho daily puzzle points at a missing lexicon entry. Date={Date} LexiconEntryId={LexiconEntryId}",
                date,
                lexiconEntryId);
            return Result<MelthoPuzzleDto>.Failure(Error.NotFound("Today's Meltho word could not be resolved."));
        }

        var dto = new MelthoPuzzleDto(
            date,
            entry.Id,
            entry.SyriacUnvocalized,
            entry.SyriacVocalized,
            entry.SblTransliteration,
            entry.PlayableLength,
            entry.Meanings.Select(m => new MelthoPuzzleMeaningDto(m.Language, m.Text)).ToArray());

        return Result<MelthoPuzzleDto>.Success(dto);
    }
}
