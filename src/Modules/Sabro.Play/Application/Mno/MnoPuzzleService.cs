using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Mno;

internal sealed class MnoPuzzleService : IMnoPuzzleService
{
    /// <summary>
    /// Recently served expressions are excluded from generation. Purely a
    /// replay guard — the equation space is enormous, so unlike Meltho's
    /// word pool this can never starve and needs no configuration.
    /// </summary>
    private const int ReplayGuardWindowDays = 365;

    private readonly PlayDbContext dbContext;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<MnoPuzzleService> logger;

    public MnoPuzzleService(
        PlayDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<MnoPuzzleService> logger)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<MnoPuzzleDto>> GetTodaysPuzzleAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var existing = await dbContext.MnoDailyPuzzles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == Games.Mno && p.Date == today, cancellationToken);
        if (existing is not null)
        {
            return Result<MnoPuzzleDto>.Success(ToDto(existing));
        }

        var cutoff = today.AddDays(-ReplayGuardWindowDays);
        var recentExpressions = await dbContext.MnoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Mno && p.Date > cutoff)
            .Select(p => p.Expression)
            .ToListAsync(cancellationToken);

        var equation = MnoEquationGenerator.Generate(Random.Shared, recentExpressions.ToHashSet());
        var puzzleResult = MnoDailyPuzzle.Create(Games.Mno, today, equation);
        if (!puzzleResult.IsSuccess)
        {
            return Result<MnoPuzzleDto>.Failure(puzzleResult.Error!);
        }

        var puzzle = puzzleResult.Value!;
        dbContext.MnoDailyPuzzles.Add(puzzle);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race on the unique (game, date) key — another request created
            // today's puzzle first. Re-read and serve their equation so every
            // player sees the same one.
            dbContext.Entry(puzzle).State = EntityState.Detached;
            var raced = await dbContext.MnoDailyPuzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == Games.Mno && p.Date == today, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return Result<MnoPuzzleDto>.Success(ToDto(raced));
        }

        logger.LogInformation(
            "Mno daily puzzle generated. Date={Date} Target={Target} Tiles={Tiles}",
            today,
            puzzle.Target,
            CountTiles(puzzle.TileForm));

        return Result<MnoPuzzleDto>.Success(ToDto(puzzle));
    }

    private static MnoPuzzleDto ToDto(MnoDailyPuzzle puzzle) =>
        new(puzzle.Date, puzzle.Target, CountTiles(puzzle.TileForm), puzzle.Expression, puzzle.TileForm);

    /// <summary>Board width of a tile form: every character is a tile except combining marks.</summary>
    private static int CountTiles(string tileForm)
    {
        var count = 0;
        foreach (var ch in tileForm)
        {
            if (ch != SyriacNumerals.Alfayo)
            {
                count++;
            }
        }

        return count;
    }
}
