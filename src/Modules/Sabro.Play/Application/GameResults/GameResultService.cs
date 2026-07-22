using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<GameResultService> logger;

    public GameResultService(
        PlayDbContext dbContext,
        IValidator<RecordGameResultRequest> recordValidator,
        TimeProvider timeProvider,
        ILogger<GameResultService> logger)
    {
        this.dbContext = dbContext;
        this.recordValidator = recordValidator;
        this.timeProvider = timeProvider;
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
}
