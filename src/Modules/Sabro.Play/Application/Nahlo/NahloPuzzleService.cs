using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabro.BethGazo.Public;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Nahlo;

internal sealed class NahloPuzzleService : INahloPuzzleService
{
    private readonly PlayDbContext dbContext;
    private readonly IChantPlayablePool playablePool;
    private readonly NahloOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<NahloPuzzleService> logger;

    public NahloPuzzleService(
        PlayDbContext dbContext,
        IChantPlayablePool playablePool,
        IOptions<NahloOptions> options,
        TimeProvider timeProvider,
        ILogger<NahloPuzzleService> logger)
    {
        this.dbContext = dbContext;
        this.playablePool = playablePool;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<NahloPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var existing = await dbContext.NahloDailyPuzzles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == Games.Nahlo && p.Date == today, cancellationToken);
        if (existing is not null)
        {
            return await RenderAsync(today, existing.ChantId, cancellationToken);
        }

        var selection = await SelectChantIdAsync(today, cancellationToken);
        if (!selection.IsSuccess)
        {
            return Result<NahloPuzzleDto>.Failure(selection.Error!);
        }

        var puzzleResult = NahloDailyPuzzle.Create(Games.Nahlo, today, selection.Value);
        if (!puzzleResult.IsSuccess)
        {
            return Result<NahloPuzzleDto>.Failure(puzzleResult.Error!);
        }

        var puzzle = puzzleResult.Value!;
        dbContext.NahloDailyPuzzles.Add(puzzle);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race on the unique (game, date) key — another request created
            // today's puzzle first. Re-read and serve their chant so every player
            // hears the same one.
            dbContext.Entry(puzzle).State = EntityState.Detached;
            var raced = await dbContext.NahloDailyPuzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == Games.Nahlo && p.Date == today, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return await RenderAsync(today, raced.ChantId, cancellationToken);
        }

        logger.LogInformation(
            "Nahlo daily puzzle selected. Date={Date} ChantId={ChantId}",
            today,
            puzzle.ChantId);

        return await RenderAsync(today, puzzle.ChantId, cancellationToken);
    }

    private async Task<Result<Guid>> SelectChantIdAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var windowDays = Math.Max(0, options.AntiRepetitionWindowDays);
        var cutoff = today.AddDays(-windowDays);

        var recentlyServed = await dbContext.NahloDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Nahlo && p.Date > cutoff)
            .Select(p => p.ChantId)
            .ToListAsync(cancellationToken);
        var excluded = recentlyServed.ToHashSet();

        var eligible = await playablePool.GetEligibleChantIdsAsync(cancellationToken);
        var candidates = eligible.Where(id => !excluded.Contains(id)).ToList();
        if (candidates.Count == 0)
        {
            logger.LogWarning(
                "Nahlo daily puzzle selection found no eligible chant. EligibleCount={EligibleCount} ExcludedCount={ExcludedCount} WindowDays={WindowDays}",
                eligible.Count,
                excluded.Count,
                windowDays);
            return Result<Guid>.Failure(Error.Conflict(
                "No eligible Nahlo chant is available for today. The playable pool may be too small for the anti-repetition window."));
        }

        var picked = candidates[Random.Shared.Next(candidates.Count)];
        return Result<Guid>.Success(picked);
    }

    private async Task<Result<NahloPuzzleDto>> RenderAsync(DateOnly date, Guid chantId, CancellationToken cancellationToken)
    {
        var chant = await playablePool.GetPlayableChantAsync(chantId, cancellationToken);
        if (chant is null)
        {
            // Also the path taken when a recorded chant has since lost its audio:
            // the pool refuses to project a chant with no recording rather than
            // handing the client a puzzle it cannot play.
            logger.LogError(
                "Nahlo daily puzzle points at a chant that cannot be rendered. Date={Date} ChantId={ChantId}",
                date,
                chantId);
            return Result<NahloPuzzleDto>.Failure(Error.NotFound("Today's Nahlo chant could not be resolved."));
        }

        var dto = new NahloPuzzleDto(
            date,
            chant.Id,
            chant.AudioUrl,
            chant.Transliteration,
            chant.SectionName,
            chant.ModeName,
            chant.VariantKind,
            chant.VariantNumber,
            chant.SyriacIncipit,
            chant.SyriacIncipitVocalized);

        return Result<NahloPuzzleDto>.Success(dto);
    }
}
