using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Play.Domain;

/// <summary>
/// Shared server state recording which chant was served as the Nahlo puzzle on a
/// given day. Selection is get-or-create per date (see the service): the first
/// request for a day picks and records the chant; every later request returns the
/// same one, so all players get an identical puzzle. One row per (game, date) —
/// the unique constraint lives in the EF configuration.
/// </summary>
public sealed class NahloDailyPuzzle : Entity<Guid>, IAggregateRoot
{
    private NahloDailyPuzzle(string gameId, DateOnly date, Guid chantId)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        GameId = gameId;
        Date = date;
        ChantId = chantId;
    }

    private NahloDailyPuzzle()
    {
    }

    /// <summary>Game discriminator, normalized to lower case (always <c>nahlo</c> today, kept for multi-game symmetry).</summary>
    public string GameId { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    public Guid ChantId { get; private set; }

    public static Result<NahloDailyPuzzle> Create(string gameId, DateOnly date, Guid chantId)
    {
        var trimmedGameId = (gameId ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmedGameId.Length == 0)
        {
            return Result<NahloDailyPuzzle>.Failure(Error.Validation("GameId is required."));
        }

        if (date == default)
        {
            return Result<NahloDailyPuzzle>.Failure(Error.Validation("Date is required."));
        }

        if (chantId == Guid.Empty)
        {
            return Result<NahloDailyPuzzle>.Failure(Error.Validation("ChantId is required."));
        }

        return Result<NahloDailyPuzzle>.Success(new NahloDailyPuzzle(trimmedGameId, date, chantId));
    }
}
