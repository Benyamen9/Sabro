namespace Sabro.Play.Application.Meltho;

/// <summary>
/// The requesting player's own standing — always returned so they can see their streak even
/// when outside the top or not opted in. <see cref="Rank"/> is null when they are not ranked
/// (not opted in, or no streak yet). <see cref="OnLeaderboard"/> reflects their opt-in choice.
/// </summary>
public sealed record MelthoLeaderboardMeDto(
    int? Rank,
    string? DisplayName,
    int LongestStreak,
    int CurrentStreak,
    bool OnLeaderboard,
    bool HasPlayed);
