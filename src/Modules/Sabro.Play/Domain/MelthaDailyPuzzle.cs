using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Play.Domain;

/// <summary>
/// Shared server state recording which lexicon entry was served as the Melthā
/// puzzle on a given day. Selection is get-or-create per date (see the service):
/// the first request for a day picks and records the word; every later request
/// returns the same one, so all players get an identical puzzle. One row per
/// (game, date) — the unique constraint lives in the EF configuration.
/// </summary>
public sealed class MelthaDailyPuzzle : Entity<Guid>, IAggregateRoot
{
    private MelthaDailyPuzzle(string gameId, DateOnly date, Guid lexiconEntryId)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        GameId = gameId;
        Date = date;
        LexiconEntryId = lexiconEntryId;
    }

    private MelthaDailyPuzzle()
    {
    }

    /// <summary>Game discriminator, normalized to lower case (always <c>meltha</c> today, kept for multi-game symmetry).</summary>
    public string GameId { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    public Guid LexiconEntryId { get; private set; }

    public static Result<MelthaDailyPuzzle> Create(string gameId, DateOnly date, Guid lexiconEntryId)
    {
        var trimmedGameId = (gameId ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmedGameId.Length == 0)
        {
            return Result<MelthaDailyPuzzle>.Failure(Error.Validation("GameId is required."));
        }

        if (date == default)
        {
            return Result<MelthaDailyPuzzle>.Failure(Error.Validation("Date is required."));
        }

        if (lexiconEntryId == Guid.Empty)
        {
            return Result<MelthaDailyPuzzle>.Failure(Error.Validation("LexiconEntryId is required."));
        }

        return Result<MelthaDailyPuzzle>.Success(new MelthaDailyPuzzle(trimmedGameId, date, lexiconEntryId));
    }
}
