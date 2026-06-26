namespace Sabro.Play.Application.Meltho;

/// <summary>The Meltho leaderboard: the top players plus the caller's own row.</summary>
public sealed record MelthoLeaderboardDto(
    IReadOnlyList<MelthoLeaderboardEntryDto> Top,
    MelthoLeaderboardMeDto Me);
