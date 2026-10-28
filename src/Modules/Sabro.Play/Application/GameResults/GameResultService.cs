using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.BethGazo.Public;
using Sabro.Play.Application.Nahlo;
using Sabro.Play.Application.Shmo;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.GameResults;

internal sealed class GameResultService : IGameResultService
{
    private readonly PlayDbContext dbContext;
    private readonly IValidator<RecordGameResultRequest> recordValidator;
    private readonly TimeProvider timeProvider;
    private readonly IChantPlayablePool chantPool;
    private readonly ILogger<GameResultService> logger;

    public GameResultService(
        PlayDbContext dbContext,
        IValidator<RecordGameResultRequest> recordValidator,
        TimeProvider timeProvider,
        IChantPlayablePool chantPool,
        ILogger<GameResultService> logger)
    {
        this.dbContext = dbContext;
        this.recordValidator = recordValidator;
        this.timeProvider = timeProvider;
        this.chantPool = chantPool;
        this.logger = logger;
    }

    public async Task<Result<RecordGameResultOutcome>> RecordAsync(string logtoUserId, RecordGameResultRequest request, CancellationToken cancellationToken)
    {
        var trimmedUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedUserId.Length == 0)
        {
            return Result<RecordGameResultOutcome>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var shapeResult = await recordValidator.ValidateAsync(request, cancellationToken);
        if (!shapeResult.IsValid)
        {
            var fields = ValidationErrorMap.FromFluentValidation(shapeResult.Errors);
            logger.LogWarning("GameResult record rejected at request validation. Fields={FieldNames}", fields.Keys);
            return Result<RecordGameResultOutcome>.Failure(Error.Validation(fields));
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (request.PlayedOn > today)
        {
            return Result<RecordGameResultOutcome>.Failure(Error.Validation("PlayedOn must not be in the future."));
        }

        await ClearPastSofroStrokesAsync(cancellationToken);

        var domainResult = GameResult.Create(
            trimmedUserId,
            request.GameId,
            request.PlayedOn,
            request.Solved,
            request.Attempts,
            request.DetailJson);
        if (!domainResult.IsSuccess)
        {
            logger.LogWarning(
                "GameResult record rejected by domain invariant. Code={ErrorCode} Message={ErrorMessage}",
                domainResult.Error!.Code,
                domainResult.Error.Message);
            return Result<RecordGameResultOutcome>.Failure(domainResult.Error!);
        }

        var result = domainResult.Value!;

        var existing = await dbContext.GameResults
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.LogtoUserId == result.LogtoUserId && r.GameId == result.GameId && r.PlayedOn == result.PlayedOn,
                cancellationToken);
        if (existing is not null)
        {
            return Result<RecordGameResultOutcome>.Success(new RecordGameResultOutcome(Map(existing), WasCreated: false));
        }

        var winError = await CheckShmoWinAsync(result, cancellationToken)
            ?? await CheckNahloWinAsync(result, cancellationToken);
        if (winError is not null)
        {
            return Result<RecordGameResultOutcome>.Failure(winError);
        }

        dbContext.GameResults.Add(result);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race on the unique (user, game, day) key — the first writer
            // wins; re-read and return their row idempotently.
            dbContext.Entry(result).State = EntityState.Detached;
            var raced = await dbContext.GameResults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.LogtoUserId == result.LogtoUserId && r.GameId == result.GameId && r.PlayedOn == result.PlayedOn,
                    cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return Result<RecordGameResultOutcome>.Success(new RecordGameResultOutcome(Map(raced), WasCreated: false));
        }

        logger.LogInformation(
            "GameResult recorded. Id={ResultId} GameId={GameId} PlayedOn={PlayedOn} Solved={Solved} Attempts={Attempts}",
            result.Id,
            result.GameId,
            result.PlayedOn,
            result.Solved,
            result.Attempts);

        return Result<RecordGameResultOutcome>.Success(new RecordGameResultOutcome(Map(result), WasCreated: true));
    }

    public async Task<Result<PagedResult<GameResultDto>>> ListForUserAsync(string logtoUserId, int page, int pageSize, string? gameId, CancellationToken cancellationToken)
    {
        var trimmedUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedUserId.Length == 0)
        {
            return Result<PagedResult<GameResultDto>>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<GameResultDto>>.Failure(pageError);
        }

        await ClearPastSofroStrokesAsync(cancellationToken);

        var query = dbContext.GameResults
            .AsNoTracking()
            .Where(r => r.LogtoUserId == trimmedUserId);

        var trimmedGameId = gameId?.Trim();
        if (!string.IsNullOrEmpty(trimmedGameId))
        {
            query = query.Where(r => r.GameId == trimmedGameId);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.PlayedOn)
            .ThenByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<GameResultDto>>.Success(
            new PagedResult<GameResultDto>(items.Select(Map).ToArray(), total, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<GameResultDto>>> ListAllForUserAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var trimmedUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedUserId.Length == 0)
        {
            return Result<IReadOnlyList<GameResultDto>>.Failure(Error.Validation("LogtoUserId is required."));
        }

        await ClearPastSofroStrokesAsync(cancellationToken);

        var items = await dbContext.GameResults
            .AsNoTracking()
            .Where(r => r.LogtoUserId == trimmedUserId)
            .OrderBy(r => r.PlayedOn)
            .ThenBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<GameResultDto>>.Success(items.Select(Map).ToArray());
    }

    public async Task<Result<int>> DeleteAllForUserAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var trimmedUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedUserId.Length == 0)
        {
            return Result<int>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var deleted = await dbContext.GameResults
            .Where(r => r.LogtoUserId == trimmedUserId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation("GameResults deleted for user. Count={DeletedCount}", deleted);
        }

        return Result<int>.Success(deleted);
    }

    private static GameResultDto Map(GameResult result) => new(
        result.Id,
        result.LogtoUserId,
        result.GameId,
        result.PlayedOn,
        result.Solved,
        result.Attempts,
        result.DetailJson,
        result.CreatedAt,
        result.UpdatedAt);

    /// <summary>
    /// Sofro keeps the strokes of the day's word (and each step's score) in its
    /// result only until the word changes at midnight UTC (Owner, 2026-10-28:
    /// "just the daily vector data, kept till the word resets"). Every Sofro
    /// result from an earlier day loses them here, for every player, before any
    /// result is recorded or read, so no earlier day's strokes are ever served.
    /// The score, word, script and vowel system stay. One cheap statement; it
    /// matches nothing once the old days are clean.
    /// </summary>
    private async Task ClearPastSofroStrokesAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var cleared = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE play.game_results
            SET detail_json = detail_json - 'strokes' - 'stepScores'
            WHERE game_id = {Games.Sofro}
              AND played_on < {today}
              AND (detail_json ? 'strokes' OR detail_json ? 'stepScores')
            """,
            cancellationToken);
        if (cleared > 0)
        {
            logger.LogInformation("Sofro strokes cleared from past days. Count={ClearedCount}", cleared);
        }
    }

    /// <summary>
    /// A solved Shmo result must end on that day's answer. A found card in the
    /// collection rests on this, and since a signed-in Shmo also sends in the rounds
    /// a device played signed out, a claimed win is checked rather than trusted.
    /// </summary>
    /// <remarks>
    /// Guesses before the win are not checked: they only make figures "met", which
    /// anyone can do in Shmo by guessing, so there is nothing to protect there.
    /// </remarks>
    private async Task<Error?> CheckShmoWinAsync(GameResult result, CancellationToken cancellationToken)
    {
        if (result.GameId != Games.Shmo || !result.Solved)
        {
            return null;
        }

        var answer = await dbContext.ShmoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Shmo && p.Date == result.PlayedOn)
            .Select(p => (Guid?)p.HistoricalFigureId)
            .FirstOrDefaultAsync(cancellationToken);
        var guesses = ShmoResultDetail.GuessIds(result.DetailJson);

        if (answer is null || guesses.Count == 0 || guesses[^1] != answer)
        {
            logger.LogWarning(
                "Shmo win rejected: it does not end on the day's answer. PlayedOn={PlayedOn} HasPuzzle={HasPuzzle} GuessCount={GuessCount}",
                result.PlayedOn,
                answer is not null,
                guesses.Count);
            return Error.Validation("A solved Shmo result must end on that day's answer.");
        }

        return null;
    }

    /// <summary>
    /// A solved Nahlo result must end on a guess that names that day's chant, by the
    /// rule Nahlo scores with (see <see cref="NahloAnswer"/>). Nahlo results feed the
    /// hub's daily-circuit streak, and a signed-in Nahlo also sends in rounds a
    /// device played signed out (Owner, 2026-10-25).
    /// </summary>
    /// <remarks>
    /// A day whose chant can no longer be read (unpublished, or its recording gone)
    /// is let through with a warning: the round was played against it, and the fault
    /// would be ours, not the player's.
    /// </remarks>
    private async Task<Error?> CheckNahloWinAsync(GameResult result, CancellationToken cancellationToken)
    {
        if (result.GameId != Games.Nahlo || !result.Solved)
        {
            return null;
        }

        var chantId = await dbContext.NahloDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Nahlo && p.Date == result.PlayedOn)
            .Select(p => (Guid?)p.ChantId)
            .FirstOrDefaultAsync(cancellationToken);
        if (chantId is null)
        {
            logger.LogWarning("Nahlo win rejected: no puzzle that day. PlayedOn={PlayedOn}", result.PlayedOn);
            return Error.Validation("A solved Nahlo result must end on that day's chant.");
        }

        var chant = await chantPool.GetPlayableChantAsync(chantId.Value, cancellationToken);
        if (chant is null)
        {
            logger.LogWarning(
                "Nahlo win accepted unchecked: the day's chant cannot be read. PlayedOn={PlayedOn} ChantId={ChantId}",
                result.PlayedOn,
                chantId);
            return null;
        }

        var guesses = NahloResultDetail.Guesses(result.DetailJson);
        if (guesses.Count == 0 || !NahloAnswer.IsRight(guesses[^1], chant))
        {
            logger.LogWarning(
                "Nahlo win rejected: it does not end on the day's chant. PlayedOn={PlayedOn} GuessCount={GuessCount}",
                result.PlayedOn,
                guesses.Count);
            return Error.Validation("A solved Nahlo result must end on that day's chant.");
        }

        return null;
    }
}
