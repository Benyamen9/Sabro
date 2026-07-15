using Sabro.Play.Domain;

namespace Sabro.UnitTests.Play.Domain;

public class MnoDailyPuzzleTests
{
    private static readonly DateOnly Date = new(2026, 7, 12);
    private static readonly MnoEquation Equation = new("12*5-8", "ܝܒ*ܗ-ܚ", 52);

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var result = MnoDailyPuzzle.Create("mno", Date, MnoDifficulty.Normal, Equation);

        result.IsSuccess.Should().BeTrue();
        var puzzle = result.Value!;
        puzzle.GameId.Should().Be("mno");
        puzzle.Date.Should().Be(Date);
        puzzle.Difficulty.Should().Be(MnoDifficulty.Normal);
        puzzle.Expression.Should().Be("12*5-8");
        puzzle.TileForm.Should().Be("ܝܒ*ܗ-ܚ");
        puzzle.Target.Should().Be(52);
        puzzle.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_NormalizesGameIdToTrimmedLowerCase()
    {
        var result = MnoDailyPuzzle.Create("  MNO ", Date, MnoDifficulty.Normal, Equation);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GameId.Should().Be("mno");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutGameId_Fails(string gameId)
    {
        var result = MnoDailyPuzzle.Create(gameId, Date, MnoDifficulty.Normal, Equation);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithDefaultDate_Fails()
    {
        var result = MnoDailyPuzzle.Create("mno", default, MnoDifficulty.Normal, Equation);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("", "ܝܒ*ܗ-ܚ", 52)]
    [InlineData("12*5-8", "", 52)]
    [InlineData("12*5-8", "ܝܒ*ܗ-ܚ", 0)]
    [InlineData("12*5-8", "ܝܒ*ܗ-ܚ", -3)]
    public void Create_WithInvalidEquation_Fails(string expression, string tileForm, int target)
    {
        var result = MnoDailyPuzzle.Create("mno", Date, MnoDifficulty.Hard, new MnoEquation(expression, tileForm, target));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
