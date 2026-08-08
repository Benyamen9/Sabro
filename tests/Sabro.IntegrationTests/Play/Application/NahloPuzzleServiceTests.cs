using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.BethGazo.Public;
using Sabro.Play.Application.Nahlo;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;

namespace Sabro.IntegrationTests.Play.Application;

// Each test pins "today" to a distinct far-future year so the shared
// nahlo_daily_puzzles table never collides on the unique (game, date) key and the
// anti-repetition window scan only ever sees this test's own seeded rows. The
// eligible pool is substituted, so selection is deterministic and decoupled from
// the shared chants table; the real PlayDbContext exercises get-or-create and the
// window scan against Postgres.
[Collection(IntegrationCollection.Name)]
public class NahloPuzzleServiceTests
{
    private readonly PostgresFixture fixture;

    public NahloPuzzleServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetTodaysPuzzle_FirstCall_SelectsFromPoolAndPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2301, 6, 15);
        var c1 = Guid.NewGuid();

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(c1), today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChantId.Should().Be(c1);
        result.Value.Date.Should().Be(today);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.NahloDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().ContainSingle().Which.ChantId.Should().Be(c1);
    }

    [Fact]
    public async Task GetTodaysPuzzle_SecondCallSameDay_ReturnsSameChant()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2311, 6, 15);
        var pool = PoolReturning(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Guid first;
        await using (var ctx = fixture.CreatePlayContext())
        {
            var r = await NewService(ctx, pool, today, windowDays: 7).GetTodaysPuzzleAsync(ct);
            first = r.Value!.ChantId;
        }

        await using var ctx2 = fixture.CreatePlayContext();
        var second = await NewService(ctx2, pool, today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        second.IsSuccess.Should().BeTrue();
        second.Value!.ChantId.Should().Be(first);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.NahloDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().ContainSingle();
    }

    [Fact]
    public async Task GetTodaysPuzzle_ExcludesChantServedWithinWindow()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2321, 6, 15);
        var served = Guid.NewGuid();
        var fresh = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), served, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(served, fresh), today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChantId.Should().Be(fresh);
    }

    [Fact]
    public async Task GetTodaysPuzzle_ChantServedBeyondWindow_IsEligibleAgain()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2331, 6, 15);
        var served = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-10), served, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(served), today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ChantId.Should().Be(served);
    }

    [Fact]
    public async Task GetTodaysPuzzle_EmptyEligiblePool_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2341, 6, 15);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(), today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task GetTodaysPuzzle_AllEligibleWithinWindow_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2351, 6, 15);
        var only = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), only, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(only), today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task GetTodaysPuzzle_WhenSelectedChantCannotBeRendered_ReturnsNotFound()
    {
        // Also the path a chant takes when it has lost its recording: the pool
        // refuses to project one with no audio rather than handing the client a
        // puzzle it cannot play.
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2361, 6, 15);
        var c1 = Guid.NewGuid();

        var pool = Substitute.For<IChantPlayablePool>();
        pool.GetEligibleChantIdsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new[] { c1 });
        pool.GetPlayableChantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlayableChant?)null);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, pool, today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetTodaysPuzzle_CarriesTheRecordingAndAllThreeAnswerParts()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2371, 6, 15);
        var c1 = Guid.NewGuid();

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(c1), today, windowDays: 7).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AudioUrl.Should().Be("/media/chants/chant.mp3");
        result.Value.Transliteration.Should().Be("Maryam yoldath Aloho");
        result.Value.ModeName.Should().Be("Tlithoyo");
        result.Value.ShuhlofoNumber.Should().Be(1);
        result.Value.SyriacIncipit.Should().Be("ܡܪܝܡ");
    }

    private static NahloPuzzleService NewService(PlayDbContext ctx, IChantPlayablePool pool, DateOnly today, int windowDays) =>
        new(
            ctx,
            pool,
            Options.Create(new NahloOptions { AntiRepetitionWindowDays = windowDays }),
            new FixedTimeProvider(today),
            NullLogger<NahloPuzzleService>.Instance);

    private static IChantPlayablePool PoolReturning(params Guid[] eligible)
    {
        var pool = Substitute.For<IChantPlayablePool>();
        pool.GetEligibleChantIdsAsync(Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)eligible);
        pool.GetPlayableChantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new PlayableChant(
                ci.Arg<Guid>(),
                "ܡܪܝܡ",
                null,
                "Maryam yoldath Aloho",
                "Farde",
                "Tlithoyo",
                1,
                "/media/chants/chant.mp3"));
        return pool;
    }

    private async Task SeedServedAsync(DateOnly date, Guid chantId, CancellationToken ct)
    {
        await using var ctx = fixture.CreatePlayContext();
        ctx.NahloDailyPuzzles.Add(NahloDailyPuzzle.Create(Games.Nahlo, date, chantId).Value!);
        await ctx.SaveChangesAsync(ct);
    }
}
