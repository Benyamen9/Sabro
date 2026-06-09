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
        var result = await NewService(read).ListForUserAsync(user, page: 1, pageSize: 50, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(3);
        result.Value.Items.Should().OnlyContain(r => r.LogtoUserId == user);
        result.Value.Items.Select(r => r.PlayedOn).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ListForUserAsync_InvalidPage_ReturnsValidation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var ctx = fixture.CreatePlayContext();

        var result = await NewService(ctx).ListForUserAsync(NewUser(), page: 0, pageSize: 50, ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    private static GameResultService NewService(PlayDbContext ctx) =>
        new(
            ctx,
            new RecordGameResultRequestValidator(),
            TimeProvider.System,
            NullLogger<GameResultService>.Instance);

    private static string NewUser() => $"play-user-{Guid.NewGuid():N}";
}
