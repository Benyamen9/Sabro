namespace Sabro.Identity.Application.UserProfiles;

/// <summary>A single user's leaderboard standing inputs: their display name (if set) and opt-in flag.</summary>
public sealed record LeaderboardMembership(string? DisplayName, bool ShowOnLeaderboard);
