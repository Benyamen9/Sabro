using System.Text.Json;
using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Play.Domain;

/// <summary>
/// A single player's outcome for one game on one day. Generic and multi-game by
/// design: keyed by Logto user id + a <see cref="GameId"/> discriminator
/// (<c>meltho</c>, later <c>shmo</c>, …) + <see cref="PlayedOn"/>. One row per
/// (user, game, day) — the unique constraint lives in the EF configuration.
/// Streaks and aggregates are derived from these rows, never stored.
/// </summary>
public sealed class GameResult : Entity<Guid>, IAggregateRoot
{
    private GameResult(string logtoUserId, string gameId, DateOnly playedOn, bool solved, int attempts, string? detailJson)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        LogtoUserId = logtoUserId;
        GameId = gameId;
        PlayedOn = playedOn;
        Solved = solved;
        Attempts = attempts;
        DetailJson = detailJson;
    }

    private GameResult()
    {
    }

    /// <summary>Opaque Logto user id (the OIDC <c>sub</c> claim) of the player.</summary>
    public string LogtoUserId { get; private set; } = string.Empty;

    /// <summary>Game discriminator, normalized to lower case (e.g. <c>meltho</c>).</summary>
    public string GameId { get; private set; } = string.Empty;

    public DateOnly PlayedOn { get; private set; }

    public bool Solved { get; private set; }

    /// <summary>Number of attempts the player used. A solved result used at least one attempt.</summary>
    public int Attempts { get; private set; }

    /// <summary>Optional game-specific extras, stored verbatim as JSON. Null when the game records nothing extra.</summary>
    public string? DetailJson { get; private set; }

    public static Result<GameResult> Create(
        string logtoUserId,
        string gameId,
        DateOnly playedOn,
        bool solved,
        int attempts,
        string? detailJson = null)
    {
        var trimmedUserId = (logtoUserId ?? string.Empty).Trim();
        if (trimmedUserId.Length == 0)
        {
            return Result<GameResult>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var gameIdResult = NormalizeGameId(gameId);
        if (!gameIdResult.IsSuccess)
        {
            return Result<GameResult>.Failure(gameIdResult.Error!);
        }

        if (playedOn == default)
        {
            return Result<GameResult>.Failure(Error.Validation("PlayedOn is required."));
        }

        if (attempts < 0)
        {
            return Result<GameResult>.Failure(Error.Validation("Attempts must not be negative."));
        }

        if (solved && attempts < 1)
        {
            return Result<GameResult>.Failure(Error.Validation("A solved result must have at least one attempt."));
        }

        var detailResult = NormalizeDetailJson(detailJson);
        if (!detailResult.IsSuccess)
        {
            return Result<GameResult>.Failure(detailResult.Error!);
        }

        return Result<GameResult>.Success(
            new GameResult(trimmedUserId, gameIdResult.Value!, playedOn, solved, attempts, detailResult.Value));
    }

    /// <summary>
    /// Trim, lower-case, and accept only <c>[a-z0-9-]</c> game ids up to 32 chars.
    /// Stored lower case so the unique (user, game, day) key is stable regardless
    /// of how a client cases the id.
    /// </summary>
    private static Result<string> NormalizeGameId(string gameId)
    {
        var trimmed = (gameId ?? string.Empty).Trim().ToLowerInvariant();
        if (trimmed.Length == 0)
        {
            return Result<string>.Failure(Error.Validation("GameId is required."));
        }

        if (trimmed.Length > 32)
        {
            return Result<string>.Failure(Error.Validation("GameId must be at most 32 characters."));
        }

        foreach (var ch in trimmed)
        {
            if (ch is not ((>= 'a' and <= 'z') or (>= '0' and <= '9') or '-'))
            {
                return Result<string>.Failure(Error.Validation("GameId may contain only lowercase letters, digits, and hyphens."));
            }
        }

        return Result<string>.Success(trimmed);
    }

    private static Result<string?> NormalizeDetailJson(string? detailJson)
    {
        if (string.IsNullOrWhiteSpace(detailJson))
        {
            return Result<string?>.Success(null);
        }

        try
        {
            JsonDocument.Parse(detailJson).Dispose();
        }
        catch (JsonException)
        {
            return Result<string?>.Failure(Error.Validation("DetailJson must be valid JSON."));
        }

        return Result<string?>.Success(detailJson);
    }
}
