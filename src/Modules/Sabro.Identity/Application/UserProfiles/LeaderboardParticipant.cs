namespace Sabro.Identity.Application.UserProfiles;

/// <summary>A leaderboard-eligible user: opaque id paired with the public display name they chose.</summary>
public sealed record LeaderboardParticipant(string LogtoUserId, string DisplayName);
