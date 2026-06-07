using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltha;

internal sealed class MelthaPuzzleService : IMelthaPuzzleService
{
    /// <summary>Hard server-side bound on playable length, re-enforced here so a mis-flagged out-of-range entry can never be served.</summary>
    private const int MinPlayableLength = 2;
    private const int MaxPlayableLength = 8;

    private readonly PlayDbContext dbContext;
    private readonly ILexiconPlayablePool playablePool;
    private readonly MelthaOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<MelthaPuzzleService> logger;

    public MelthaPuzzleService(
        PlayDbContext dbContext,
        ILexiconPlayablePool playablePool,
        IOptions<MelthaOptions> options,
        TimeProvider timeProvider,
        ILogger<MelthaPuzzleService> logger)
    {
        this.dbContext = dbContext;
        this.playablePool = playablePool;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<MelthaPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var existing = await dbContext.MelthaDailyPuzzles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == Games.Meltha && p.Date == today, cancellationToken);
        if (existing is not null)
        {
            return await RenderAsync(today, existing.LexiconEntryId, cancellationToken);
        }

        var selection = await SelectEntryIdAsync(today, cancellationToken);
        if (!selection.IsSuccess)
        {
            return Result<MelthaPuzzleDto>.Failure(selection.Error!);
        }

        var puzzleResult = MelthaDailyPuzzle.Create(Games.Meltha, today, selection.Value);
        if (!puzzleResult.IsSuccess)
        {
            return Result<MelthaPuzzleDto>.Failure(puzzleResult.Error!);
        }

        var puzzle = puzzleResult.Value!;
        dbContext.MelthaDailyPuzzles.Add(puzzle);
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
            var raced = await dbContext.MelthaDailyPuzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == Games.Meltha && p.Date == today, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return await RenderAsync(today, raced.LexiconEntryId, cancellationToken);
        }

        logger.LogInformation(
            "Melthā daily puzzle selected. Date={Date} LexiconEntryId={LexiconEntryId}",
            today,
            puzzle.LexiconEntryId);

        return await RenderAsync(today, puzzle.LexiconEntryId, cancellationToken);
    }

    private async Task<Result<Guid>> SelectEntryIdAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var windowDays = Math.Max(0, options.AntiRepetitionWindowDays);
        var cutoff = today.AddDays(-windowDays);

        var recentlyServed = await dbContext.MelthaDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Meltha && p.Date > cutoff)
            .Select(p => p.LexiconEntryId)
            .ToListAsync(cancellationToken);
        var excluded = recentlyServed.ToHashSet();

        var eligible = await playablePool.GetEligibleEntryIdsAsync(MinPlayableLength, MaxPlayableLength, cancellationToken);
        var candidates = eligible.Where(id => !excluded.Contains(id)).ToList();
        if (candidates.Count == 0)
        {
            logger.LogWarning(
                "Melthā daily puzzle selection found no eligible word. EligibleCount={EligibleCount} ExcludedCount={ExcludedCount} WindowDays={WindowDays}",
                eligible.Count,
                excluded.Count,
                windowDays);
            return Result<Guid>.Failure(Error.Conflict(
                "No eligible Melthā word is available for today. The playable pool may be too small for the anti-repetition window."));
        }

        var picked = candidates[Random.Shared.Next(candidates.Count)];
        return Result<Guid>.Success(picked);
    }

    private async Task<Result<MelthaPuzzleDto>> RenderAsync(DateOnly date, Guid lexiconEntryId, CancellationToken cancellationToken)
    {
        var entry = await playablePool.GetPlayableEntryAsync(lexiconEntryId, cancellationToken);
        if (entry is null)
        {
            logger.LogError(
                "Melthā daily puzzle points at a missing lexicon entry. Date={Date} LexiconEntryId={LexiconEntryId}",
                date,
                lexiconEntryId);
            return Result<MelthaPuzzleDto>.Failure(Error.NotFound("Today's Melthā word could not be resolved."));
        }

        var dto = new MelthaPuzzleDto(
            date,
            entry.Id,
            entry.SyriacUnvocalized,
            entry.SyriacVocalized,
            entry.SblTransliteration,
            entry.PlayableLength,
            entry.Meanings.Select(m => new MelthaPuzzleMeaningDto(m.Language, m.Text)).ToArray());

        return Result<MelthaPuzzleDto>.Success(dto);
    }
}
