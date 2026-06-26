using Microsoft.EntityFrameworkCore;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltho;

internal sealed class MelthoLeaderboardService : IMelthoLeaderboardService
{
    /// <summary>How many ranked rows the board returns.</summary>
    private const int TopCount = 10;

    private readonly PlayDbContext dbContext;
    private readonly ILeaderboardDirectory directory;

    public MelthoLeaderboardService(PlayDbContext dbContext, ILeaderboardDirectory directory)
    {
        this.dbContext = dbContext;
        this.directory = directory;
    }

    public async Task<Result<MelthoLeaderboardDto>> GetAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var callerId = (logtoUserId ?? string.Empty).Trim();
        if (callerId.Length == 0)
        {
            return Result<MelthoLeaderboardDto>.Failure(Error.Validation("LogtoUserId is required."));
        }

        var participants = await directory.GetParticipantsAsync(cancellationToken);

        // Compute streaks for the opted-in roster plus the caller (who may not be opted in,
        // but must still see their own streak). One query, grouped in memory.
        var userIds = participants.Select(p => p.LogtoUserId).Append(callerId).Distinct().ToList();
        var streaks = await ComputeStreaksAsync(userIds, cancellationToken);

        // Rank opted-in players who have at least a 1-day streak. Deterministic tie-break:
        // longer current streak, then display name, then id.
        var ranked = participants
            .Select(p => new
            {
                p.LogtoUserId,
                p.DisplayName,
                Streak = streaks.GetValueOrDefault(p.LogtoUserId, new MelthoStreak(0, 0)),
            })
            .Where(x => x.Streak.Longest >= 1)
            .OrderByDescending(x => x.Streak.Longest)
            .ThenByDescending(x => x.Streak.Current)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.LogtoUserId, StringComparer.Ordinal)
            .ToList();

        var top = ranked
            .Take(TopCount)
            .Select((x, index) => new MelthoLeaderboardEntryDto(
                Rank: index + 1,
                DisplayName: x.DisplayName,
                LongestStreak: x.Streak.Longest,
                CurrentStreak: x.Streak.Current,
                IsMe: x.LogtoUserId == callerId))
            .ToList();

        var membership = await directory.GetMembershipAsync(callerId, cancellationToken);
        var callerStreak = streaks.GetValueOrDefault(callerId, new MelthoStreak(0, 0));
        var callerRankIndex = ranked.FindIndex(x => x.LogtoUserId == callerId);

        var me = new MelthoLeaderboardMeDto(
            Rank: callerRankIndex >= 0 ? callerRankIndex + 1 : null,
            DisplayName: membership?.DisplayName,
            LongestStreak: callerStreak.Longest,
            CurrentStreak: callerStreak.Current,
            OnLeaderboard: membership?.ShowOnLeaderboard ?? false,
            HasPlayed: streaks.ContainsKey(callerId));

        return Result<MelthoLeaderboardDto>.Success(new MelthoLeaderboardDto(top, me));
    }

    private async Task<Dictionary<string, MelthoStreak>> ComputeStreaksAsync(
        List<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<string, MelthoStreak>();
        }

        var rows = await dbContext.GameResults
            .AsNoTracking()
            .Where(r => r.GameId == Games.Meltho && userIds.Contains(r.LogtoUserId))
            .Select(r => new { r.LogtoUserId, r.PlayedOn, r.Solved })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.LogtoUserId)
            .ToDictionary(
                group => group.Key,
                group => MelthoStreaks.Compute(group.Select(r => (r.PlayedOn, r.Solved))));
    }
}
