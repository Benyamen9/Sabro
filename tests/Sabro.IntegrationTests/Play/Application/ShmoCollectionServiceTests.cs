using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sabro.BethGazo.Public;
using Sabro.Historical.Domain;
using Sabro.Historical.Public;
using Sabro.Play.Application.GameResults;
using Sabro.Play.Application.Shmo;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;

namespace Sabro.IntegrationTests.Play.Application;

/// <summary>
/// The player's figure collection against real Postgres, and the check that a
/// recorded Shmo win ends on that day's answer.
/// </summary>
/// <remarks>
/// Each test seeds its own puzzle days at random dates in the 19th century, clear
/// of the far-future years <see cref="ShmoPuzzleServiceTests"/> uses, so the shared
/// unique (game, date) key never collides.
/// </remarks>
[Collection(IntegrationCollection.Name)]
public class ShmoCollectionServiceTests
{
    private readonly PostgresFixture fixture;

    public ShmoCollectionServiceTests(PostgresFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task RecordAsync_ShmoWinEndingOnTheAnswer_IsAccepted()
    {
        var ct = TestContext.Current.CancellationToken;
        var answer = Guid.NewGuid();
        var day = await SeedPuzzleAsync(answer, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewResultService(ctx).RecordAsync(NewUser(), ShmoResult(day, solved: true, Guid.NewGuid(), answer), ct);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RecordAsync_ShmoWinNotEndingOnTheAnswer_IsRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        var answer = Guid.NewGuid();
        var day = await SeedPuzzleAsync(answer, ct);

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewResultService(ctx).RecordAsync(NewUser(), ShmoResult(day, solved: true, answer, Guid.NewGuid()), ct);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public async Task RecordAsync_ShmoWinOnADayWithNoPuzzle_IsRejected()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewResultService(ctx).RecordAsync(NewUser(), ShmoResult(RandomDay(), solved: true, Guid.NewGuid()), ct);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RecordAsync_ShmoRoundGivenUp_IsNotChecked()
    {
        // A round given up claims nothing; its guesses only make figures "met".
        var ct = TestContext.Current.CancellationToken;

        await using var ctx = fixture.CreatePlayContext();
        var result = await NewResultService(ctx).RecordAsync(NewUser(), ShmoResult(RandomDay(), solved: false, Guid.NewGuid()), ct);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetForUser_FoundCardCarriesTheStoryAndMetCardDoesNot()
    {
        var ct = TestContext.Current.CancellationToken;
        var found = await SeedFigureAsync("Deacon of Nisibis and Edessa.", ct);
        var met = await SeedFigureAsync("A figure only guessed.", ct);
        var day = await SeedPuzzleAsync(found, ct);
        var user = NewUser();

        await using (var ctx = fixture.CreatePlayContext())
        {
            var recorded = await NewResultService(ctx).RecordAsync(user, ShmoResult(day, solved: true, met, found), ct);
            recorded.IsSuccess.Should().BeTrue();
        }

        await using var read = fixture.CreatePlayContext();
        await using var historical = fixture.CreateHistoricalContext();
        var collection = await new ShmoCollectionService(read, new HistoricalFigureDescriptions(historical))
            .GetForUserAsync(user, ct);

        collection.IsSuccess.Should().BeTrue();
        var cards = collection.Value!.Cards;
        cards.Should().HaveCount(2);

        var foundCard = cards.Single(c => c.HistoricalFigureId == found);
        foundCard.Status.Should().Be(ShmoCardStatus.Found);
        foundCard.FirstFoundOn.Should().Be(day);
        foundCard.FewestAttempts.Should().Be(2);
        foundCard.Descriptions.Should().ContainSingle().Which.Text.Should().Be("Deacon of Nisibis and Edessa.");

        var metCard = cards.Single(c => c.HistoricalFigureId == met);
        metCard.Status.Should().Be(ShmoCardStatus.Met);
        metCard.Descriptions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetForUser_OnlyReadsTheCallersOwnRounds()
    {
        var ct = TestContext.Current.CancellationToken;
        var answer = Guid.NewGuid();
        var day = await SeedPuzzleAsync(answer, ct);

        await using (var ctx = fixture.CreatePlayContext())
        {
            await NewResultService(ctx).RecordAsync(NewUser(), ShmoResult(day, solved: true, answer), ct);
        }

        await using var read = fixture.CreatePlayContext();
        await using var historical = fixture.CreateHistoricalContext();
        var collection = await new ShmoCollectionService(read, new HistoricalFigureDescriptions(historical))
            .GetForUserAsync(NewUser(), ct);

        collection.Value!.Cards.Should().BeEmpty();
    }

    private static RecordGameResultRequest ShmoResult(DateOnly day, bool solved, params Guid[] guesses) =>
        new(
            Games.Shmo,
            day,
            solved,
            guesses.Length,
            $"{{\"guessIds\":[{string.Join(',', guesses.Select(g => $"\"{g}\""))}]}}");

    private static GameResultService NewResultService(PlayDbContext ctx) =>
        new(ctx, new RecordGameResultRequestValidator(), TimeProvider.System, Substitute.For<IChantPlayablePool>(), NullLogger<GameResultService>.Instance);

    private static string NewUser() => $"shmo-collector-{Guid.NewGuid():N}";

    private static DateOnly RandomDay() => new DateOnly(1800, 1, 1).AddDays(Random.Shared.Next(0, 36_500));

    private async Task<DateOnly> SeedPuzzleAsync(Guid answer, CancellationToken ct)
    {
        var day = RandomDay();
        await using var ctx = fixture.CreatePlayContext();
        ctx.ShmoDailyPuzzles.Add(ShmoDailyPuzzle.Create(Games.Shmo, day, answer).Value!);
        await ctx.SaveChangesAsync(ct);
        return day;
    }

    private async Task<Guid> SeedFigureAsync(string englishDescription, CancellationToken ct)
    {
        var figure = HistoricalFigure.Create(
            $"Collected Figure {Guid.NewGuid():N}",
            HistoricalFigureCategory.Patristic,
            4,
            HistoricalPeriod.NiceneEra,
            HistoricalFigureRole.Commentator,
            HistoricalFigureRegion.Mesopotamia,
            HistoricalFigureGender.Male).Value!;
        figure.ReplaceDescriptions([HistoricalFigureDescription.Create("en", englishDescription).Value!]);

        await using var ctx = fixture.CreateHistoricalContext();
        ctx.Figures.Add(figure);
        await ctx.SaveChangesAsync(ct);
        return figure.Id;
    }
}
