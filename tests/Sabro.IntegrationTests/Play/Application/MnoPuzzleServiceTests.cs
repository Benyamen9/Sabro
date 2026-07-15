using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sabro.Play.Application.Mno;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;

namespace Sabro.IntegrationTests.Play.Application;

// Each test pins "today" to a distinct far-future year so the shared
// mno_daily_puzzles table never collides on the unique (game, date, difficulty)
// key. Generation is random by design; the tests assert the persisted-and-
// shared contract (get-or-create per level) and the served puzzle's
// invariants, not a specific equation.
[Collection(IntegrationCollection.Name)]
public class MnoPuzzleServiceTests
{
    private readonly PostgresFixture fixture;

    public MnoPuzzleServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task GetTodaysPuzzle_FirstCall_GeneratesAndPersists()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2201, 7, 12);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, today).GetTodaysPuzzleAsync(MnoDifficulty.Normal, ct);

        result.IsSuccess.Should().BeTrue();
        var puzzle = result.Value!;
        puzzle.Date.Should().Be(today);
        puzzle.Difficulty.Should().Be("normal");
        puzzle.Target.Should().BeGreaterThanOrEqualTo(1);
        puzzle.TileCount.Should().Be(MnoEquationGenerator.TileWidth);
        puzzle.Expression.Should().MatchRegex(@"^\d+([+\-*/]\d+){1,2}$");
        puzzle.TileForm.Should().NotBeNullOrWhiteSpace();

        await using var read = fixture.CreatePlayContext();
        var rows = await read.MnoDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        var row = rows.Should().ContainSingle().Which;
        row.Expression.Should().Be(puzzle.Expression);
        row.TileForm.Should().Be(puzzle.TileForm);
        row.Target.Should().Be(puzzle.Target);
        row.GameId.Should().Be(Games.Mno);
        row.Difficulty.Should().Be(MnoDifficulty.Normal);
    }

    [Fact]
    public async Task GetTodaysPuzzle_SecondCallSameDay_ReturnsSameEquation()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2211, 7, 12);

        string firstExpression;
        await using (var ctx = fixture.CreatePlayContext())
        {
            var first = await NewService(ctx, today).GetTodaysPuzzleAsync(MnoDifficulty.Normal, ct);
            firstExpression = first.Value!.Expression;
        }

        await using var ctx2 = fixture.CreatePlayContext();
        var second = await NewService(ctx2, today).GetTodaysPuzzleAsync(MnoDifficulty.Normal, ct);

        second.IsSuccess.Should().BeTrue();
        second.Value!.Expression.Should().Be(firstExpression);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.MnoDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().ContainSingle();
    }

    [Fact]
    public async Task GetTodaysPuzzle_EachDifficulty_IsItsOwnSharedDaily()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2231, 7, 12);

        // Every level of the ladder gets its own get-or-create row for the same
        // date, and asking a level again returns that level's equation.
        var served = new Dictionary<MnoDifficulty, string>();
        foreach (var difficulty in Enum.GetValues<MnoDifficulty>())
        {
            await using var ctx = fixture.CreatePlayContext();
            var result = await NewService(ctx, today).GetTodaysPuzzleAsync(difficulty, ct);

            result.IsSuccess.Should().BeTrue();
            result.Value!.Difficulty.Should().Be(difficulty.ToString().ToLowerInvariant());
            served[difficulty] = result.Value!.Expression;
        }

        await using var reread = fixture.CreatePlayContext();
        var again = await NewService(reread, today).GetTodaysPuzzleAsync(MnoDifficulty.Extreme, ct);
        again.Value!.Expression.Should().Be(served[MnoDifficulty.Extreme]);

        await using var read = fixture.CreatePlayContext();
        var rows = await read.MnoDailyPuzzles.Where(p => p.Date == today).ToListAsync(ct);
        rows.Should().HaveCount(Enum.GetValues<MnoDifficulty>().Length);
        rows.Select(r => r.Difficulty).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetTodaysPuzzle_DoesNotRepeatARecentlyServedExpression()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2221, 7, 12);

        // Seed yesterday's puzzle, then ask for today's: the replay guard must
        // pass yesterday's expression to the generator's exclusion set, so the
        // two days can never serve the same equation.
        string yesterdayExpression;
        await using (var seedCtx = fixture.CreatePlayContext())
        {
            var seeded = await NewService(seedCtx, today.AddDays(-1)).GetTodaysPuzzleAsync(MnoDifficulty.Normal, ct);
            yesterdayExpression = seeded.Value!.Expression;
        }

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, today).GetTodaysPuzzleAsync(MnoDifficulty.Normal, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Expression.Should().NotBe(yesterdayExpression);
    }

    private static MnoPuzzleService NewService(PlayDbContext ctx, DateOnly today) =>
        new(ctx, new FixedTimeProvider(today), NullLogger<MnoPuzzleService>.Instance);
}
