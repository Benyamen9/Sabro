using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Public;
using Sabro.Play.Application.GameResults;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;

namespace Sabro.IntegrationTests.Play.Application;

/// <summary>
/// Recording a solved Nahlo result checks that its last guess names that day's
/// chant. Puzzle days are seeded at random dates in the 18th century, clear of the
/// years the puzzle-selection tests use.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class NahloWinCheckTests
{
    private const string RightGuess = "{\"guesses\":[{\"melody\":\"Mo rhimin\",\"mode\":\"Qadmoyo\",\"isShuhlofo\":false},{\"melody\":\"Zodeq dnehwe\",\"mode\":\"Tlithoyo\",\"isShuhlofo\":false}]}";
    private const string WrongGuess = "{\"guesses\":[{\"melody\":\"Zodeq dnehwe\",\"mode\":\"Qadmoyo\",\"isShuhlofo\":false}]}";

    private readonly PostgresFixture fixture;

    public NahloWinCheckTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task RecordAsync_WinEndingOnTheChant_IsAccepted()
    {
        var ct = TestContext.Current.CancellationToken;
        var (day, pool) = await SeedDayAsync(ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, pool).RecordAsync(NewUser(), Nahlo(day, solved: true, RightGuess), ct);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAsync_WinNotEndingOnTheChant_IsRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var (day, pool) = await SeedDayAsync(ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, pool).RecordAsync(NewUser(), Nahlo(day, solved: true, WrongGuess), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task RecordAsync_WinOnADayWithNoPuzzle_IsRejected()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, Substitute.For<IChantPlayablePool>())
            .RecordAsync(NewUser(), Nahlo(RandomDay(), solved: true, RightGuess), ct);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RecordAsync_LostRound_IsNotChecked()
    {
        var ct = TestContext.Current.CancellationToken;
        var (day, pool) = await SeedDayAsync(ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, pool).RecordAsync(NewUser(), Nahlo(day, solved: false, WrongGuess), ct);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAsync_WhenTheChantCanNoLongerBeRead_LetsTheWinThrough()
    {
        // The fault would be ours (a chant unpublished or its recording gone), not the player's.
        var ct = TestContext.Current.CancellationToken;
        var day = RandomDay();
        await using (var seed = fixture.CreatePlayContext())
        {
            seed.NahloDailyPuzzles.Add(NahloDailyPuzzle.Create(Games.Nahlo, day, Guid.NewGuid()).Value!);
            await seed.SaveChangesAsync(ct);
        }

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewService(ctx, Substitute.For<IChantPlayablePool>())
            .RecordAsync(NewUser(), Nahlo(day, solved: true, WrongGuess), ct);

        result.IsSuccess.Should().BeTrue();
    }

    private static RecordGameResultRequest Nahlo(DateOnly day, bool solved, string detail) =>
        new(Games.Nahlo, day, solved, 2, detail);

    private static GameResultService NewService(PlayDbContext ctx, IChantPlayablePool pool) =>
        new(ctx, new RecordGameResultRequestValidator(), TimeProvider.System, pool, NullLogger<GameResultService>.Instance);

    private static string NewUser() => $"nahlo-player-{Guid.NewGuid():N}";

    private static DateOnly RandomDay() => new DateOnly(1700, 1, 1).AddDays(Random.Shared.Next(0, 36_500));

    private async Task<(DateOnly Day, IChantPlayablePool Pool)> SeedDayAsync(CancellationToken ct)
    {
        var day = RandomDay();
        var chantId = Guid.NewGuid();
        await using (var ctx = fixture.CreatePlayContext())
        {
            ctx.NahloDailyPuzzles.Add(NahloDailyPuzzle.Create(Games.Nahlo, day, chantId).Value!);
            await ctx.SaveChangesAsync(ct);
        }

        var pool = Substitute.For<IChantPlayablePool>();
        pool.GetPlayableChantAsync(chantId, Arg.Any<CancellationToken>()).Returns(new PlayableChant(
            chantId, null, null, "Zodeq dnehwe", "Qole shahroye", "Tlithoyo", ChantVariantKind.None, null, "/media/chants/x.mp3"));
        return (day, pool);
    }
}
