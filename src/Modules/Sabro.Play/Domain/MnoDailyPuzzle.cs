using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Play.Domain;

/// <summary>
/// Shared server state recording which equation was served as the Mno puzzle
/// on a given day. Selection is get-or-create per date (see the service): the
/// first request for a day generates and records the equation; every later
/// request returns the same one, so all players get an identical puzzle. The
/// tile form is persisted rather than re-derived because a value can have
/// several valid Syriac spellings — the stored form is the day's board, for
/// everyone. One row per (game, date, difficulty): each level of the ladder is
/// its own shared daily puzzle. The unique constraint lives in the EF
/// configuration.
/// </summary>
public sealed class MnoDailyPuzzle : Entity<Guid>, IAggregateRoot
{
    private MnoDailyPuzzle(string gameId, DateOnly date, MnoDifficulty difficulty, string expression, string tileForm, int target)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        GameId = gameId;
        Date = date;
        Difficulty = difficulty;
        Expression = expression;
        TileForm = tileForm;
        Target = target;
    }

    private MnoDailyPuzzle()
    {
    }

    /// <summary>Game discriminator, normalized to lower case (always <c>mno</c> today, kept for multi-game symmetry).</summary>
    public string GameId { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    /// <summary>The ladder level this daily belongs to — each level is its own shared puzzle.</summary>
    public MnoDifficulty Difficulty { get; private set; }

    /// <summary>The solution in neutral ASCII, e.g. <c>12*5-8</c>.</summary>
    public string Expression { get; private set; } = string.Empty;

    /// <summary>The exact Syriac board form of the solution — the generator's chosen spelling.</summary>
    public string TileForm { get; private set; } = string.Empty;

    /// <summary>The number the equation equals; what the player is shown.</summary>
    public int Target { get; private set; }

    public static Result<MnoDailyPuzzle> Create(string gameId, DateOnly date, MnoDifficulty difficulty, MnoEquation equation)
    {
        var trimmedGameId = (gameId ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmedGameId.Length == 0)
        {
            return Result<MnoDailyPuzzle>.Failure(Error.Validation("GameId is required."));
        }

        if (date == default)
        {
            return Result<MnoDailyPuzzle>.Failure(Error.Validation("Date is required."));
        }

        if (string.IsNullOrWhiteSpace(equation.Expression))
        {
            return Result<MnoDailyPuzzle>.Failure(Error.Validation("Expression is required."));
        }

        if (string.IsNullOrWhiteSpace(equation.TileForm))
        {
            return Result<MnoDailyPuzzle>.Failure(Error.Validation("TileForm is required."));
        }

        if (equation.Target < 1)
        {
            return Result<MnoDailyPuzzle>.Failure(Error.Validation("Target must be at least 1."));
        }

        return Result<MnoDailyPuzzle>.Success(new MnoDailyPuzzle(trimmedGameId, date, difficulty, equation.Expression, equation.TileForm, equation.Target));
    }
}
