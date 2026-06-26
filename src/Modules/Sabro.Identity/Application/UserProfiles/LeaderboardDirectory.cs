using Microsoft.EntityFrameworkCore;
using Sabro.Identity.Infrastructure;

namespace Sabro.Identity.Application.UserProfiles;

internal sealed class LeaderboardDirectory : ILeaderboardDirectory
{
    private readonly IdentityDbContext dbContext;

    public LeaderboardDirectory(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LeaderboardParticipant>> GetParticipantsAsync(CancellationToken cancellationToken)
    {
        // Opted in AND has a name. The opt-in invariant already requires a name, but
        // guard here too so a future data drift can never surface a nameless row.
        var rows = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.ShowOnLeaderboard && p.DisplayName != null && p.DisplayName != string.Empty)
            .Select(p => new LeaderboardParticipant(p.LogtoUserId, p.DisplayName!))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<LeaderboardMembership?> GetMembershipAsync(string logtoUserId, CancellationToken cancellationToken)
    {
        var trimmed = (logtoUserId ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return await dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.LogtoUserId == trimmed)
            .Select(p => new LeaderboardMembership(p.DisplayName, p.ShowOnLeaderboard))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
