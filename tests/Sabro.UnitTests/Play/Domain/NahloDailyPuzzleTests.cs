using Sabro.Play.Domain;

namespace Sabro.UnitTests.Play.Domain;

public class NahloDailyPuzzleTests
{
    private static readonly DateOnly Date = new(2026, 8, 5);

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var chantId = Guid.NewGuid();

        var result = NahloDailyPuzzle.Create("nahlo", Date, chantId);

        result.IsSuccess.Should().BeTrue();
        var puzzle = result.Value!;
        puzzle.GameId.Should().Be("nahlo");
        puzzle.Date.Should().Be(Date);
        puzzle.ChantId.Should().Be(chantId);
        puzzle.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_NormalizesGameIdToTrimmedLowerCase()
    {
        var result = NahloDailyPuzzle.Create("  NAHLO ", Date, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value!.GameId.Should().Be("nahlo");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutGameId_Fails(string gameId)
    {
        var result = NahloDailyPuzzle.Create(gameId, Date, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithDefaultDate_Fails()
    {
        var result = NahloDailyPuzzle.Create("nahlo", default, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithEmptyChantId_Fails()
    {
        var result = NahloDailyPuzzle.Create("nahlo", Date, Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
