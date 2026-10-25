using Sabro.Play.Application.Shmo;

namespace Sabro.UnitTests.Play.Application;

/// <summary>
/// How recorded Shmo rounds become a player's collection: met by guessing (or by
/// seeing the answer of a round given up), found by solving.
/// </summary>
public class ShmoCollectionTests
{
    private const string Player = "logto-user-1";

    private static readonly Guid Peter = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid Andrew = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid Ephrem = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    [Fact]
    public void Build_WithNoRounds_IsEmpty()
    {
        ShmoCollection.Build(Player, []).Should().BeEmpty();
    }

    [Fact]
    public void Build_SolvedRound_MeetsTheGuessesAndFindsTheAnswer()
    {
        var cards = ShmoCollection.Build(Player, [Solved(Day(1), Peter, Andrew, Ephrem)]);

        cards.Should().HaveCount(3);
        Card(cards, Ephrem).Found.Should().BeTrue();
        Card(cards, Ephrem).FirstFoundOn.Should().Be(Day(1));
        Card(cards, Ephrem).FewestAttempts.Should().Be(3);
        Card(cards, Peter).Found.Should().BeFalse();
        Card(cards, Andrew).Found.Should().BeFalse();
    }

    [Fact]
    public void Build_RoundGivenUp_MeetsTheRevealedAnswerButDoesNotFindIt()
    {
        var cards = ShmoCollection.Build(Player, [GivenUp(Day(1), answer: Ephrem, Peter)]);

        Card(cards, Ephrem).Found.Should().BeFalse();
        Card(cards, Ephrem).TimesMet.Should().Be(1);
        Card(cards, Peter).TimesMet.Should().Be(1);
    }

    [Fact]
    public void Build_CountsEachDayAFigureWasMetOnce()
    {
        // Peter guessed on two days, and guessed twice on the first: two meetings.
        var cards = ShmoCollection.Build(
            Player,
            [
                new ShmoPlayedDay(Day(1), Solved: true, Attempts: 3, [Peter, Peter, Ephrem], Ephrem),
                Solved(Day(2), Peter, Andrew),
            ]);

        Card(cards, Peter).TimesMet.Should().Be(2);
    }

    [Fact]
    public void Build_FoundTwice_KeepsTheFirstDayAndTheFewestAttempts()
    {
        var cards = ShmoCollection.Build(
            Player,
            [
                Solved(Day(40), Ephrem),
                Solved(Day(3), Peter, Andrew, Peter, Andrew, Ephrem),
            ]);

        Card(cards, Ephrem).FirstFoundOn.Should().Be(Day(3));
        Card(cards, Ephrem).FewestAttempts.Should().Be(1);
        Card(cards, Ephrem).TimesMet.Should().Be(2);
    }

    [Fact]
    public void Build_DayWithoutAPuzzleRow_StillMeetsTheGuessesButFindsNothing()
    {
        var cards = ShmoCollection.Build(
            Player,
            [new ShmoPlayedDay(Day(1), Solved: true, Attempts: 2, [Peter, Ephrem], AnswerId: null)]);

        cards.Should().OnlyContain(c => !c.Found);
        cards.Select(c => c.FigureId).Should().BeEquivalentTo([Peter, Ephrem]);
    }

    [Fact]
    public void Build_Illuminated_FollowsTheFindNotTheGuess()
    {
        var day = FirstIlluminatedDay(Player, Ephrem);

        var found = ShmoCollection.Build(Player, [Solved(day, Ephrem)]);
        var onlyMet = ShmoCollection.Build(Player, [GivenUp(day, answer: Peter, Ephrem)]);

        Card(found, Ephrem).Illuminated.Should().BeTrue();
        Card(onlyMet, Ephrem).Illuminated.Should().BeFalse();
    }

    [Fact]
    public void Illumination_IsTheSameEveryTimeForTheSameFind()
    {
        var first = ShmoIllumination.IsIlluminated(Player, Ephrem, Day(7));
        var again = ShmoIllumination.IsIlluminated(Player, Ephrem, Day(7));

        again.Should().Be(first);
    }

    [Fact]
    public void Illumination_ComesOutAboutOneInTwenty()
    {
        // 20,000 finds across players, figures and days. One in twenty is 1,000;
        // the bounds sit far outside any honest run's spread.
        var hits = 0;
        for (var i = 0; i < 20_000; i++)
        {
            if (ShmoIllumination.IsIlluminated($"player-{i % 50}", Guid.NewGuid(), Day(i % 365)))
            {
                hits++;
            }
        }

        hits.Should().BeInRange(800, 1200);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{\"guessIds\":\"nope\"}")]
    [InlineData("{\"other\":[]}")]
    public void GuessIds_FromAMissingOrMalformedDetail_IsEmpty(string? detail)
    {
        ShmoResultDetail.GuessIds(detail).Should().BeEmpty();
    }

    [Fact]
    public void GuessIds_KeepsThePlayedOrderAndSkipsBadEntries()
    {
        var detail = $"{{\"guessIds\":[\"{Peter}\",42,\"x\",\"{Ephrem}\"]}}";

        ShmoResultDetail.GuessIds(detail).Should().Equal(Peter, Ephrem);
    }

    private static DateOnly Day(int n) => new DateOnly(2026, 1, 1).AddDays(n);

    private static ShmoPlayedDay Solved(DateOnly date, params Guid[] guesses) =>
        new(date, Solved: true, Attempts: guesses.Length, guesses, AnswerId: guesses[^1]);

    private static ShmoPlayedDay GivenUp(DateOnly date, Guid answer, params Guid[] guesses) =>
        new(date, Solved: false, Attempts: guesses.Length, guesses, answer);

    private static ShmoCollection.Card Card(IReadOnlyList<ShmoCollection.Card> cards, Guid figureId) =>
        cards.Single(c => c.FigureId == figureId);

    private static DateOnly FirstIlluminatedDay(string player, Guid figureId)
    {
        for (var n = 0; n < 2000; n++)
        {
            if (ShmoIllumination.IsIlluminated(player, figureId, Day(n)))
            {
                return Day(n);
            }
        }

        throw new InvalidOperationException("No illuminated day in 2,000 tries.");
    }
}
