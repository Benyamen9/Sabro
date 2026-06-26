namespace Sabro.Identity.Application.UserProfiles;

/// <summary>
/// Read-only cross-module surface over the leaderboard-eligible users, consumed by the
/// Play module to attach human-readable names to streaks. Identity owns who is opted in
/// and how they appear; Play owns the streaks. Kept narrow on purpose — callers get the
/// opted-in roster and a single-user membership lookup, never the full profile surface.
/// </summary>
public interface ILeaderboardDirectory
{
    /// <summary>
    /// Every user who has opted in to the leaderboard and has a non-empty display name.
    /// Order is unspecified — the caller ranks by streak.
    /// </summary>
    Task<IReadOnlyList<LeaderboardParticipant>> GetParticipantsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The display name and opt-in flag for one user, or <c>null</c> if they have no profile.
    /// Used to build the caller's own leaderboard row even when they are outside the top.
    /// </summary>
    Task<LeaderboardMembership?> GetMembershipAsync(string logtoUserId, CancellationToken cancellationToken);
}
