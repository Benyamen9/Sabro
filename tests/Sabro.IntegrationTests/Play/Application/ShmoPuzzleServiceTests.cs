using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.Historical.Domain;
using Sabro.Historical.Public;
using Sabro.Play.Application.Shmo;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;

namespace Sabro.IntegrationTests.Play.Application;

// Each test pins "today" to a distinct far-future year so the shared
// shmo_daily_puzzles table never collides on the unique (game, date) key and the
// anti-repetition window scan only ever sees this test's own seeded rows. The
// eligible pool is substituted, so selection is deterministic and decoupled from
// the shared historical_figures table; the real PlayDbContext exercises
// get-or-create and the window scan against Postgres.
[Collection(IntegrationCollection.Name)]
public class ShmoPuzzleServiceTests
{
    private readonly PostgresFixture fixture;

    public ShmoPuzzleServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetTodaysPuzzle_FirstCall_SelectsFromPoolAndPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2201, 6, 15);
        var f1 = Guid.NewGuid();

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(f1), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalFigureId.Should().Be(f1);
        result.Value.Date.Should().Be(today);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.ShmoDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().ContainSingle().Which.HistoricalFigureId.Should().Be(f1);
    }

    [Fact]
    public async Task GetTodaysPuzzle_SecondCallSameDay_ReturnsSameFigure()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2211, 6, 15);
        var pool = PoolReturning(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Guid first;
        await using (var ctx = fixture.CreatePlayContext())
        {
            var r = await NewService(ctx, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct);
            first = r.Value!.HistoricalFigureId;
        }

        await using var ctx2 = fixture.CreatePlayContext();
        var second = await NewService(ctx2, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        second.IsSuccess.Should().BeTrue();
        second.Value!.HistoricalFigureId.Should().Be(first);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.ShmoDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().ContainSingle();
    }

    [Fact]
    public async Task GetTodaysPuzzle_ExcludesFigureServedWithinWindow()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2221, 6, 15);
        var served = Guid.NewGuid();
        var fresh = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), served, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(served, fresh), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalFigureId.Should().Be(fresh);
    }

    [Fact]
    public async Task GetTodaysPuzzle_FigureServedBeyondWindow_IsEligibleAgain()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2231, 6, 15);
        var served = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-40), served, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(served), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.HistoricalFigureId.Should().Be(served);
    }

    [Fact]
    public async Task GetTodaysPuzzle_EmptyEligiblePool_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2241, 6, 15);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task GetTodaysPuzzle_AllEligibleWithinWindow_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2251, 6, 15);
        var only = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), only, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(only), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task GetTodaysPuzzle_WhenSelectedFigureMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2261, 6, 15);
        var f1 = Guid.NewGuid();

        var pool = Substitute.For<IHistoricalFigurePlayablePool>();
        pool.GetEligibleFigureIdsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new[] { f1 });
        pool.GetPlayableFigureAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlayableHistoricalFigure?)null);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetTodaysPuzzle_CarriesEveryScoredAttribute()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2271, 6, 15);
        var f1 = Guid.NewGuid();

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(f1), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Jacob of Edessa");
        result.Value.Category.Should().Be(HistoricalFigureCategory.Patristic);
        result.Value.Era.Should().Be(7);
        result.Value.Role.Should().Be(HistoricalFigureRole.Bishop);
        result.Value.Region.Should().Be(HistoricalFigureRegion.Syria);
        result.Value.Tradition.Should().Be(HistoricalFigureTradition.WestSyriac);
        result.Value.Gender.Should().Be(HistoricalFigureGender.Male);
    }

    private static ShmoPuzzleService NewService(PlayDbContext ctx, IHistoricalFigurePlayablePool pool, DateOnly today, int windowDays) =>
        new(
            ctx,
            pool,
            Options.Create(new ShmoOptions { AntiRepetitionWindowDays = windowDays }),
            new FixedTimeProvider(today),
            NullLogger<ShmoPuzzleService>.Instance);

    private static IHistoricalFigurePlayablePool PoolReturning(params Guid[] eligible)
    {
        var pool = Substitute.For<IHistoricalFigurePlayablePool>();
        pool.GetEligibleFigureIdsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)eligible);
        pool.GetPlayableFigureAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new PlayableHistoricalFigure(
                ci.Arg<Guid>(),
                "Jacob of Edessa",
                HistoricalFigureCategory.Patristic,
                7,
                HistoricalFigureRole.Bishop,
                HistoricalFigureRegion.Syria,
                HistoricalFigureTradition.WestSyriac,
                HistoricalFigureGender.Male));
        return pool;
    }

    private async Task SeedServedAsync(DateOnly date, Guid figureId, CancellationToken ct)
    {
        await using var ctx = fixture.CreatePlayContext();
        ctx.ShmoDailyPuzzles.Add(ShmoDailyPuzzle.Create(Games.Shmo, date, figureId).Value!);
        await ctx.SaveChangesAsync(ct);
    }
}
