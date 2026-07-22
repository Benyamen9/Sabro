using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Play.Application.GameResults;
using Sabro.Play.Infrastructure;

namespace Sabro.IntegrationTests.Play.Application;

[Collection(IntegrationCollection.Name)]
public class GameResultServiceTests
{
    private readonly PostgresFixture fixture;

    public GameResultServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task RecordAsync_FirstCall_CreatesAndPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = NewUser();
        await using var ctx = fixture.CreatePlayContext();
        var service = NewService(ctx);

        var result = await service.RecordAsync(
            user,
            new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 7), Solved: true, Attempts: 3, DetailJson: null),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasCreated.Should().BeTrue();
        result.Value.Result.LogtoUserId.Should().Be(user);
        result.Value.Result.Attempts.Should().Be(3);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.GameResults.Where(r => r.LogtoUserId == user).ToListAsync(ct);
        rows.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordAsync_SecondCallSameDay_IsIdempotentAndKeepsFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = NewUser();
        var playedOn = new DateOnly(2026, 6, 7);

        await using (var ctx = fixture.CreatePlayContext())
        {
            var first = await NewService(ctx).RecordAsync(
                user,
                new RecordGameResultRequest("meltho", playedOn, Solved: true, Attempts: 3, DetailJson: null),
                ct);
            first.Value!.WasCreated.Should().BeTrue();
        }

        await using var second = fixture.CreatePlayContext();
        var result = await NewService(second).RecordAsync(
            user,
            new RecordGameResultRequest("meltho", playedOn, Solved: false, Attempts: 6, DetailJson: null),
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasCreated.Should().BeFalse();
        result.Value.Result.Solved.Should().BeTrue();
        result.Value.Result.Attempts.Should().Be(3);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.GameResults.Where(r => r.LogtoUserId == user).ToListAsync(ct);
        rows.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordAsync_FutureDate_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx).RecordAsync(
            NewUser(),
            new RecordGameResultRequest("meltho", future, Solved: false, Attempts: 0, DetailJson: null),
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task RecordAsync_InvalidGameId_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx).RecordAsync(
            NewUser(),
            new RecordGameResultRequest("not a game!", new DateOnly(2026, 6, 7), Solved: false, Attempts: 0, DetailJson: null),
            ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task ListForUserAsync_ReturnsOnlyOwnResults_NewestDayFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = NewUser();
        var other = NewUser();

        await using (var ctx = fixture.CreatePlayContext())
        {
            var service = NewService(ctx);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 5), true, 2, null), ct);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 7), true, 4, null), ct);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 6), false, 6, null), ct);
            await service.RecordAsync(other, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 7), true, 1, null), ct);
        }

        await using var read = fixture.CreatePlayContext();
        var result = await NewService(read).ListForUserAsync(user, page: 1, pageSize: 50, gameId: null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(3);
        result.Value.Items.Should().OnlyContain(r => r.LogtoUserId == user);
        result.Value.Items.Select(r => r.PlayedOn).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListForUserAsync_FilteredByGameId_ReturnsOnlyThatGame()
    {
        // Regression guard: a user with results on more than one game must be
        // able to ask "did I already play THIS game today" without a same-day
        // result from another game being mistaken for it (reported live on Mno,
        // where a Meltho result from earlier the same day was surfaced as an
        // already-played Mno puzzle).
        var ct = TestContext.Current.CancellationToken;
        var user = NewUser();

        await using (var ctx = fixture.CreatePlayContext())
        {
            var service = NewService(ctx);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 7), true, 4, null), ct);
            await service.RecordAsync(user, new RecordGameResultRequest("mno", new DateOnly(2026, 6, 7), true, 2, null), ct);
        }

        await using var read = fixture.CreatePlayContext();
        var result = await NewService(read).ListForUserAsync(user, page: 1, pageSize: 50, gameId: "mno", ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle(r => r.GameId == "mno" && r.Attempts == 2);
    }

    [Fact]
    public async Task ListForUserAsync_InvalidPage_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx).ListForUserAsync(NewUser(), page: 0, pageSize: 50, gameId: null, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task DeleteAllForUserAsync_RemovesOnlyOwnResults_AndReportsCount()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = NewUser();
        var other = NewUser();

        await using (var ctx = fixture.CreatePlayContext())
        {
            var service = NewService(ctx);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 5), true, 2, null), ct);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 6), false, 6, null), ct);
            await service.RecordAsync(other, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 7), true, 1, null), ct);
        }

        await using var del = fixture.CreatePlayContext();
        var result = await NewService(del).DeleteAllForUserAsync(user, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);

        await using var read = fixture.CreatePlayContext();
        (await read.GameResults.AnyAsync(r => r.LogtoUserId == user, ct)).Should().BeFalse();
        (await read.GameResults.AnyAsync(r => r.LogtoUserId == other, ct)).Should().BeTrue();
    }

    [Fact]
    public async Task ListAllForUserAsync_ReturnsOnlyOwnResults_OldestDayFirst()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = NewUser();
        var other = NewUser();

        await using (var ctx = fixture.CreatePlayContext())
        {
            var service = NewService(ctx);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 6), false, 6, null), ct);
            await service.RecordAsync(user, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 5), true, 2, null), ct);
            await service.RecordAsync(other, new RecordGameResultRequest("meltho", new DateOnly(2026, 6, 7), true, 1, null), ct);
        }

        await using var read = fixture.CreatePlayContext();
        var result = await NewService(read).ListAllForUserAsync(user, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value!.Select(r => r.PlayedOn).Should().ContainInOrder(new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 6));
        result.Value!.Should().OnlyContain(r => r.LogtoUserId == user);
    }

    [Fact]
    public async Task ListAllForUserAsync_NoResults_ReturnsEmpty()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx).ListAllForUserAsync(NewUser(), ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAllForUserAsync_NoResults_ReturnsZero()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx).DeleteAllForUserAsync(NewUser(), ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    private static GameResultService NewService(PlayDbContext ctx) =>
        new(
            ctx,
            new RecordGameResultRequestValidator(),
            TimeProvider.System,
            NullLogger<GameResultService>.Instance);

    private static string NewUser() => $"play-user-{Guid.NewGuid():N}";
}
