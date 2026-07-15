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
    /// word pool this can never starve and needs no configuration. The guard
    /// spans every ladder level, so no level repeats another's recent equation.
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

    public async Task<Result<MnoPuzzleDto>> GetTodaysPuzzleAsync(MnoDifficulty difficulty, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var existing = await dbContext.MnoDailyPuzzles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GameId == Games.Mno && p.Date == today && p.Difficulty == difficulty, cancellationToken);
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

        var equation = MnoEquationGenerator.Generate(difficulty, Random.Shared, recentExpressions.ToHashSet());
        var puzzleResult = MnoDailyPuzzle.Create(Games.Mno, today, difficulty, equation);
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
            // Lost a race on the unique (game, date, difficulty) key — another
            // request created this level's puzzle first. Re-read and serve their
            // equation so every player on the level sees the same one.
            dbContext.Entry(puzzle).State = EntityState.Detached;
            var raced = await dbContext.MnoDailyPuzzles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.GameId == Games.Mno && p.Date == today && p.Difficulty == difficulty, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return Result<MnoPuzzleDto>.Success(ToDto(raced));
        }

        logger.LogInformation(
            "Mno daily puzzle generated. Date={Date} Difficulty={Difficulty} Target={Target} Tiles={Tiles}",
            today,
            difficulty,
            puzzle.Target,
            SyriacNumerals.TileCountOf(puzzle.TileForm));

        return Result<MnoPuzzleDto>.Success(ToDto(puzzle));
    }

    private static MnoPuzzleDto ToDto(MnoDailyPuzzle puzzle) =>
        new(
            puzzle.Date,
            puzzle.Difficulty.ToString().ToLowerInvariant(),
            puzzle.Target,
            SyriacNumerals.TileCountOf(puzzle.TileForm),
            puzzle.Expression,
            puzzle.TileForm);
}
