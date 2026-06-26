using Microsoft.EntityFrameworkCore;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Play.Application.Meltho;
using Sabro.Play.Domain;

namespace Sabro.IntegrationTests.Play.Application;

// The leaderboard aggregates globally across all opted-in profiles, so each test first clears
// the user_profiles and meltho game_results tables to get a deterministic view (mirrors the
// library tests). The IntegrationCollection runs sequentially.
[Collection(IntegrationCollection.Name)]
public class MelthoLeaderboardServiceTests
{
    private readonly PostgresFixture fixture;

    public MelthoLeaderboardServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetAsync_RanksOptedInPlayersByLongestStreak_AndExcludesOptedOut()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);

        var ephrem = NewUser();
        var jacob = NewUser();
        var hidden = NewUser();
        await SeedProfileAsync(ephrem, "Ephrem", showOnLeaderboard: true, ct);
        await SeedProfileAsync(jacob, "Jacob", showOnLeaderboard: true, ct);
        await SeedProfileAsync(hidden, "Hidden", showOnLeaderboard: false, ct);
        await SeedConsecutiveWinsAsync(ephrem, new DateOnly(2026, 5, 1), 3, ct); // longest 3
        await SeedConsecutiveWinsAsync(jacob, new DateOnly(2026, 5, 1), 5, ct); // longest 5
        await SeedConsecutiveWinsAsync(hidden, new DateOnly(2026, 5, 1), 7, ct); // opted out → excluded

        var result = await GetLeaderboardAsync(ephrem, ct);

        result.IsSuccess.Should().BeTrue();
        var board = result.Value!;
        board.Top.Should().HaveCount(2);
        board.Top.Select(e => e.DisplayName).Should().Equal("Jacob", "Ephrem");
        board.Top.Select(e => e.Rank).Should().Equal(1, 2);
        board.Top[0].LongestStreak.Should().Be(5);
        board.Top[0].IsMe.Should().BeFalse();
        board.Top[1].IsMe.Should().BeTrue(); // Ephrem is the caller

        board.Me.Rank.Should().Be(2);
        board.Me.LongestStreak.Should().Be(3);
        board.Me.OnLeaderboard.Should().BeTrue();
        board.Me.HasPlayed.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_OptedOutCaller_SeesOwnStreakButNoRank()
    {
        var ct = TestContext.Current.CancellationToken;
        await ClearAsync(ct);

        var jacob = NewUser();
        var shy = NewUser();
        await SeedProfileAsync(jacob, "Jacob", showOnLeaderboard: true, ct);
        await SeedProfileAsync(shy, "Shy", showOnLeaderboard: false, ct);
        await SeedConsecutiveWinsAsync(jacob, new DateOnly(2026, 5, 1), 4, ct);
        await SeedConsecutiveWinsAsync(shy, new DateOnly(2026, 5, 1), 6, ct);

        var result = await GetLeaderboardAsync(shy, ct);

        result.IsSuccess.Should().BeTrue();
        var board = result.Value!;
        board.Top.Should().ContainSingle().Which.DisplayName.Should().Be("Jacob");
        board.Me.Rank.Should().BeNull();
        board.Me.OnLeaderboard.Should().BeFalse();
        board.Me.LongestStreak.Should().Be(6); // still sees their own streak
        board.Me.HasPlayed.Should().BeTrue();
    }

    private static string NewUser() => $"lb-user-{Guid.NewGuid():N}";

    private async Task<Sabro.Shared.Results.Result<MelthoLeaderboardDto>> GetLeaderboardAsync(string callerId, CancellationToken ct)
    {
        await using var playCtx = fixture.CreatePlayContext();
        await using var identityCtx = fixture.CreateIdentityContext();
        var service = new MelthoLeaderboardService(playCtx, new LeaderboardDirectory(identityCtx));
        return await service.GetAsync(callerId, ct);
    }

    private async Task SeedProfileAsync(string logtoUserId, string displayName, bool showOnLeaderboard, CancellationToken ct)
    {
        await using var ctx = fixture.CreateIdentityContext();
        var profile = UserProfile.Create(logtoUserId).Value!;
        profile.UpdateAccount(displayName, showOnLeaderboard).Should().BeNull();
        ctx.UserProfiles.Add(profile);
        await ctx.SaveChangesAsync(ct);
    }

    private async Task SeedConsecutiveWinsAsync(string logtoUserId, DateOnly start, int days, CancellationToken ct)
    {
        await using var ctx = fixture.CreatePlayContext();
        for (var i = 0; i < days; i++)
        {
            var result = GameResult.Create(logtoUserId, Games.Meltho, start.AddDays(i), solved: true, attempts: 3).Value!;
            ctx.GameResults.Add(result);
        }

        await ctx.SaveChangesAsync(ct);
    }

    private async Task ClearAsync(CancellationToken ct)
    {
        await using (var play = fixture.CreatePlayContext())
        {
            await play.GameResults.Where(r => r.GameId == Games.Meltho).ExecuteDeleteAsync(ct);
        }

        await using var identity = fixture.CreateIdentityContext();
        await identity.UserProfiles.ExecuteDeleteAsync(ct);
    }
}
