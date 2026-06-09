using Sabro.Play.Domain;

namespace Sabro.UnitTests.Play.Domain;

public class MelthoDailyPuzzleTests
{
    private static readonly DateOnly Date = new(2026, 6, 7);

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var entryId = Guid.NewGuid();

        var result = MelthoDailyPuzzle.Create("meltho", Date, entryId);

        result.IsSuccess.Should().BeTrue();
        var puzzle = result.Value!;
        puzzle.GameId.Should().Be("meltho");
        puzzle.Date.Should().Be(Date);
        puzzle.LexiconEntryId.Should().Be(entryId);
        puzzle.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_NormalizesGameIdToTrimmedLowerCase()
    {
        var result = MelthoDailyPuzzle.Create("  MELTHO ", Date, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value!.GameId.Should().Be("meltho");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutGameId_Fails(string gameId)
    {
        var result = MelthoDailyPuzzle.Create(gameId, Date, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithDefaultDate_Fails()
    {
        var result = MelthoDailyPuzzle.Create("meltho", default, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithEmptyLexiconEntryId_Fails()
    {
        var result = MelthoDailyPuzzle.Create("meltho", Date, Guid.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
