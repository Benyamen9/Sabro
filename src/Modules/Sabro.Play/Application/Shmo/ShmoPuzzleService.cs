using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sabro.Historical.Public;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Shmo;

internal sealed class ShmoPuzzleService : IShmoPuzzleService
{
    private readonly PlayDbContext dbContext;
    private readonly IHistoricalFigurePlayablePool playablePool;
    private readonly ShmoOptions options;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ShmoPuzzleService> logger;

    public ShmoPuzzleService(
        PlayDbContext dbContext,
        IHistoricalFigurePlayablePool playablePool,
        IOptions<ShmoOptions> options,
        TimeProvider timeProvider,
        ILogger<ShmoPuzzleService> logger)
    {
        this.dbContext = dbContext;
        this.playablePool = playablePool;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<ShmoPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var existing = await dbContext.ShmoDailyPuzzles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == Games.Shmo && p.Date == today, cancellationToken);
        if (existing is not null)
        {
            return await RenderAsync(today, existing.HistoricalFigureId, cancellationToken);
        }

        var selection = await SelectFigureIdAsync(today, cancellationToken);
        if (!selection.IsSuccess)
        {
            return Result<ShmoPuzzleDto>.Failure(selection.Error!);
        }

        var puzzleResult = ShmoDailyPuzzle.Create(Games.Shmo, today, selection.Value);
        if (!puzzleResult.IsSuccess)
        {
            return Result<ShmoPuzzleDto>.Failure(puzzleResult.Error!);
        }

        var puzzle = puzzleResult.Value!;
        dbContext.ShmoDailyPuzzles.Add(puzzle);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race on the unique (game, date) key — another request created
            // today's puzzle first. Re-read and serve their figure so every player
            // sees the same one.
            dbContext.Entry(puzzle).State = EntityState.Detached;
            var raced = await dbContext.ShmoDailyPuzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == Games.Shmo && p.Date == today, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return await RenderAsync(today, raced.HistoricalFigureId, cancellationToken);
        }

        logger.LogInformation(
            "Shmo daily puzzle selected. Date={Date} HistoricalFigureId={HistoricalFigureId}",
            today,
            puzzle.HistoricalFigureId);

        return await RenderAsync(today, puzzle.HistoricalFigureId, cancellationToken);
    }

    private async Task<Result<Guid>> SelectFigureIdAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var windowDays = Math.Max(0, options.AntiRepetitionWindowDays);
        var cutoff = today.AddDays(-windowDays);

        var recentlyServed = await dbContext.ShmoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Shmo && p.Date > cutoff)
            .Select(p => p.HistoricalFigureId)
            .ToListAsync(cancellationToken);
        var excluded = recentlyServed.ToHashSet();

        var eligible = await playablePool.GetEligibleFigureIdsAsync(cancellationToken);
        var candidates = eligible.Where(id => !excluded.Contains(id)).ToList();
        if (candidates.Count == 0)
        {
            logger.LogWarning(
                "Shmo daily puzzle selection found no eligible figure. EligibleCount={EligibleCount} ExcludedCount={ExcludedCount} WindowDays={WindowDays}",
                eligible.Count,
                excluded.Count,
                windowDays);
            return Result<Guid>.Failure(Error.Conflict(
                "No eligible Shmo figure is available for today. The playable roster may be too small for the anti-repetition window."));
        }

        var picked = candidates[Random.Shared.Next(candidates.Count)];
        return Result<Guid>.Success(picked);
    }

    private async Task<Result<ShmoPuzzleDto>> RenderAsync(DateOnly date, Guid historicalFigureId, CancellationToken cancellationToken)
    {
        var figure = await playablePool.GetPlayableFigureAsync(historicalFigureId, cancellationToken);
        if (figure is null)
        {
            logger.LogError(
                "Shmo daily puzzle points at a missing historical figure. Date={Date} HistoricalFigureId={HistoricalFigureId}",
                date,
                historicalFigureId);
            return Result<ShmoPuzzleDto>.Failure(Error.NotFound("Today's Shmo figure could not be resolved."));
        }

        var dto = new ShmoPuzzleDto(
            date,
            figure.Id,
            figure.Name,
            figure.Category,
            figure.Era,
            figure.Period,
            figure.Role,
            figure.Region,
            figure.Tradition,
            figure.Gender);

        return Result<ShmoPuzzleDto>.Success(dto);
    }
}
