using Sabro.Play.Domain;

namespace Sabro.UnitTests.Play.Domain;

public class GameResultTests
{
    private static readonly DateOnly PlayedOn = new(2026, 6, 7);

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var result = GameResult.Create("user-1", "meltha", PlayedOn, solved: true, attempts: 3, detailJson: "{\"guesses\":3}");

        result.IsSuccess.Should().BeTrue();
        var entry = result.Value!;
        entry.LogtoUserId.Should().Be("user-1");
        entry.GameId.Should().Be("meltha");
        entry.PlayedOn.Should().Be(PlayedOn);
        entry.Solved.Should().BeTrue();
        entry.Attempts.Should().Be(3);
        entry.DetailJson.Should().Be("{\"guesses\":3}");
        entry.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithoutLogtoUserId_Fails(string? userId)
    {
        var result = GameResult.Create(userId!, "meltha", PlayedOn, solved: false, attempts: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutGameId_Fails(string gameId)
    {
        var result = GameResult.Create("user-1", gameId, PlayedOn, solved: false, attempts: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("MELTHA", "meltha")]
    [InlineData("  Shmo  ", "shmo")]
    public void Create_NormalizesGameIdToTrimmedLowerCase(string input, string expected)
    {
        var result = GameResult.Create("user-1", input, PlayedOn, solved: false, attempts: 1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.GameId.Should().Be(expected);
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("UPPER_under")]
    [InlineData("emoji😀")]
    public void Create_WithDisallowedGameIdCharacters_Fails(string gameId)
    {
        var result = GameResult.Create("user-1", gameId, PlayedOn, solved: false, attempts: 1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithTooLongGameId_Fails()
    {
        var result = GameResult.Create("user-1", new string('a', 33), PlayedOn, solved: false, attempts: 1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNegativeAttempts_Fails()
    {
        var result = GameResult.Create("user-1", "meltha", PlayedOn, solved: false, attempts: -1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_SolvedWithZeroAttempts_Fails()
    {
        var result = GameResult.Create("user-1", "meltha", PlayedOn, solved: true, attempts: 0);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_UnsolvedWithZeroAttempts_Succeeds()
    {
        var result = GameResult.Create("user-1", "meltha", PlayedOn, solved: false, attempts: 0);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithDefaultPlayedOn_Fails()
    {
        var result = GameResult.Create("user-1", "meltha", default, solved: false, attempts: 1);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithBlankDetailJson_StoresNull(string? detailJson)
    {
        var result = GameResult.Create("user-1", "meltha", PlayedOn, solved: false, attempts: 1, detailJson: detailJson);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DetailJson.Should().BeNull();
    }

    [Fact]
    public void Create_WithMalformedDetailJson_Fails()
    {
        var result = GameResult.Create("user-1", "meltha", PlayedOn, solved: false, attempts: 1, detailJson: "{not json");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
