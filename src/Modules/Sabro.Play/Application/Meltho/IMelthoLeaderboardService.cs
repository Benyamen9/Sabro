using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltho;

public interface IMelthoLeaderboardService
{
    /// <summary>
    /// Builds the Meltho leaderboard for the signed-in caller: the top players by longest
    /// streak (opted-in users only) plus the caller's own standing. The caller's streak is
    /// always computed, even when they have not opted in or are outside the top.
    /// </summary>
    Task<Result<MelthoLeaderboardDto>> GetAsync(string logtoUserId, CancellationToken cancellationToken);
}
