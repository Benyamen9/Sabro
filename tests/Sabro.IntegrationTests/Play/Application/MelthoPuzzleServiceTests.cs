using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Application.Meltho;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;

namespace Sabro.IntegrationTests.Play.Application;

// Each test pins "today" to a distinct far-future year so the shared
// meltho_daily_puzzles table never collides on the unique (game, date) key and
// the anti-repetition window scan only ever sees this test's own seeded rows.
// The eligible pool is substituted, so selection is deterministic and decoupled
// from the shared lexicon table; the real PlayDbContext exercises get-or-create
// and the window scan against Postgres.
[Collection(IntegrationCollection.Name)]
public class MelthoPuzzleServiceTests
{
    private readonly PostgresFixture fixture;

    public MelthoPuzzleServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetTodaysPuzzle_FirstCall_SelectsFromPoolAndPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2101, 6, 15);
        var e1 = Guid.NewGuid();

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(e1), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LexiconEntryId.Should().Be(e1);
        result.Value.Date.Should().Be(today);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.MelthoDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().ContainSingle().Which.LexiconEntryId.Should().Be(e1);
    }

    [Fact]
    public async Task GetTodaysPuzzle_SecondCallSameDay_ReturnsSameEntry()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2111, 6, 15);
        var pool = PoolReturning(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Guid first;
        await using (var ctx = fixture.CreatePlayContext())
        {
            var r = await NewService(ctx, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct);
            first = r.Value!.LexiconEntryId;
        }

        await using var ctx2 = fixture.CreatePlayContext();
        var second = await NewService(ctx2, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        second.IsSuccess.Should().BeTrue();
        second.Value!.LexiconEntryId.Should().Be(first);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.MelthoDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().ContainSingle();
    }

    [Fact]
    public async Task GetTodaysPuzzle_ExcludesEntryServedWithinWindow()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2121, 6, 15);
        var served = Guid.NewGuid();
        var fresh = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), served, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(served, fresh), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LexiconEntryId.Should().Be(fresh);
    }

    [Fact]
    public async Task GetTodaysPuzzle_EntryServedBeyondWindow_IsEligibleAgain()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2131, 6, 15);
        var served = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-40), served, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(served), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LexiconEntryId.Should().Be(served);
    }

    [Fact]
    public async Task GetTodaysPuzzle_EmptyEligiblePool_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2141, 6, 15);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task GetTodaysPuzzle_AllEligibleWithinWindow_ReturnsConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2151, 6, 15);
        var only = Guid.NewGuid();
        await SeedServedAsync(today.AddDays(-1), only, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, PoolReturning(only), today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task GetTodaysPuzzle_WhenSelectedEntryMissing_ReturnsNotFound()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2161, 6, 15);
        var e1 = Guid.NewGuid();

        var pool = Substitute.For<ILexiconPlayablePool>();
        pool.GetEligibleEntryIdsAsync(2, 8, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)new[] { e1 });
        pool.GetPlayableEntryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlayableLexiconEntry?)null);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("not_found");
    }

    /// <summary>
    /// The day number a player reads: "Meltho #51".
    /// </summary>
    /// <remarks>
    /// Asserted as a <b>difference</b> rather than an absolute. The number counts
    /// from the first Meltho puzzle ever recorded, and these tests share one table,
    /// so any absolute value would depend on which other test seeded the earliest
    /// row. The difference is independent of that, and is the property worth
    /// pinning anyway.
    /// </remarks>
    [Fact]
    public async Task GetTodaysPuzzle_DayNumber_CountsCalendarDaysNotPuzzles()
    {
        // The bug this exists for lived in the client: Meltho showed
        // "distinct past words + 1", which was only true while nothing repeated.
        // Once the pool was exhausted it fell behind the real day and printed the
        // same number on consecutive days. The number must track the calendar.
        var ct = TestContext.Current.CancellationToken;
        var first = new DateOnly(2171, 6, 15);
        var later = first.AddDays(10);

        int firstNumber;
        await using (var ctx = fixture.CreatePlayContext())
        {
            var r = await NewService(ctx, PoolReturning(Guid.NewGuid()), first, windowDays: 0).GetTodaysPuzzleAsync(ct);
            firstNumber = r.Value!.DayNumber;
        }

        await using var ctx2 = fixture.CreatePlayContext();
        var second = await NewService(ctx2, PoolReturning(Guid.NewGuid()), later, windowDays: 0).GetTodaysPuzzleAsync(ct);

        // Ten calendar days later reads ten higher — even though only two puzzles
        // were served between them. Counting rows would have said "+1".
        second.Value!.DayNumber.Should().Be(firstNumber + 10);
    }

    [Fact]
    public async Task GetTodaysPuzzle_DayNumber_IsStableForTheSameDay()
    {
        // Two players opening the game on the same day must read the same number,
        // which is the whole reason this is server-side and not computed client-side.
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2181, 6, 15);
        var pool = PoolReturning(Guid.NewGuid());

        int first;
        await using (var ctx = fixture.CreatePlayContext())
        {
            first = (await NewService(ctx, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct)).Value!.DayNumber;
        }

        await using var ctx2 = fixture.CreatePlayContext();
        var again = await NewService(ctx2, pool, today, windowDays: 30).GetTodaysPuzzleAsync(ct);

        again.Value!.DayNumber.Should().Be(first);
    }

    [Fact]
    public async Task GetTodaysPuzzle_DayNumber_IsUnchangedWhenAWordRepeats()
    {
        // A repeat is now the normal case — the launch pool is exhausted — and it
        // must still advance the counter by exactly one day.
        var ct = TestContext.Current.CancellationToken;
        var day = new DateOnly(2191, 6, 15);
        var onlyWord = Guid.NewGuid();

        int before;
        await using (var ctx = fixture.CreatePlayContext())
        {
            before = (await NewService(ctx, PoolReturning(onlyWord), day, windowDays: 0).GetTodaysPuzzleAsync(ct)).Value!.DayNumber;
        }

        await using var ctx2 = fixture.CreatePlayContext();
        var next = await NewService(ctx2, PoolReturning(onlyWord), day.AddDays(1), windowDays: 0).GetTodaysPuzzleAsync(ct);

        next.Value!.LexiconEntryId.Should().Be(onlyWord, "the same word repeats when the pool holds only one");
        next.Value.DayNumber.Should().Be(before + 1);
    }

    private static MelthoPuzzleService NewService(PlayDbContext ctx, ILexiconPlayablePool pool, DateOnly today, int windowDays) =>
        new(
            ctx,
            pool,
            Options.Create(new MelthoOptions { AntiRepetitionWindowDays = windowDays }),
            new FixedTimeProvider(today),
            NullLogger<MelthoPuzzleService>.Instance);

    private static ILexiconPlayablePool PoolReturning(params Guid[] eligible)
    {
        var pool = Substitute.For<ILexiconPlayablePool>();
        pool.GetEligibleEntryIdsAsync(2, 8, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Guid>)eligible);
        pool.GetPlayableEntryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new PlayableLexiconEntry(ci.Arg<Guid>(), "ܐܒ", null, null, "Noun", 2, Array.Empty<LexiconMeaningDto>()));
        return pool;
    }

    private async Task SeedServedAsync(DateOnly date, Guid entryId, CancellationToken ct)
    {
        await using var ctx = fixture.CreatePlayContext();
        ctx.MelthoDailyPuzzles.Add(MelthoDailyPuzzle.Create(Games.Meltho, date, entryId).Value!);
        await ctx.SaveChangesAsync(ct);
    }
}
