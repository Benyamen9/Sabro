using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Play.Domain;

/// <summary>
/// Shared server state recording which historical figure was served as the Shmo
/// puzzle on a given day. Selection is get-or-create per date (see the service):
/// the first request for a day picks and records the figure; every later request
/// returns the same one, so all players get an identical puzzle. One row per
/// (game, date) — the unique constraint lives in the EF configuration.
/// </summary>
public sealed class ShmoDailyPuzzle : Entity<Guid>, IAggregateRoot
{
    private ShmoDailyPuzzle(string gameId, DateOnly date, Guid historicalFigureId)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        GameId = gameId;
        Date = date;
        HistoricalFigureId = historicalFigureId;
    }

    private ShmoDailyPuzzle()
    {
    }

    /// <summary>Game discriminator, normalized to lower case (always <c>shmo</c> today, kept for multi-game symmetry).</summary>
    public string GameId { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    public Guid HistoricalFigureId { get; private set; }

    public static Result<ShmoDailyPuzzle> Create(string gameId, DateOnly date, Guid historicalFigureId)
    {
        var trimmedGameId = (gameId ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmedGameId.Length == 0)
        {
            return Result<ShmoDailyPuzzle>.Failure(Error.Validation("GameId is required."));
        }

        if (date == default)
        {
            return Result<ShmoDailyPuzzle>.Failure(Error.Validation("Date is required."));
        }

        if (historicalFigureId == Guid.Empty)
        {
            return Result<ShmoDailyPuzzle>.Failure(Error.Validation("HistoricalFigureId is required."));
        }

        return Result<ShmoDailyPuzzle>.Success(new ShmoDailyPuzzle(trimmedGameId, date, historicalFigureId));
    }
}
