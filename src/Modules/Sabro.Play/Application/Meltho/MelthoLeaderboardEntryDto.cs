namespace Sabro.Play.Application.Meltho;

/// <summary>One ranked row on the Meltho leaderboard.</summary>
public sealed record MelthoLeaderboardEntryDto(
    int Rank,
    string DisplayName,
    int LongestStreak,
    int CurrentStreak,
    bool IsMe);
