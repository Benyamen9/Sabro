using FluentValidation.TestHelper;
using Sabro.Play.Application.GameResults;

namespace Sabro.UnitTests.Play.Application;

public class RecordGameResultRequestValidatorTests
{
    private readonly RecordGameResultRequestValidator validator = new();

    [Fact]
    public void ValidInput_HasNoErrors()
    {
        var result = validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MissingGameId_HasError(string? gameId)
    {
        var result = validator.TestValidate(Valid() with { GameId = gameId! });

        result.ShouldHaveValidationErrorFor(x => x.GameId);
    }

    [Fact]
    public void TooLongGameId_HasError()
    {
        var result = validator.TestValidate(Valid() with { GameId = new string('a', 33) });

        result.ShouldHaveValidationErrorFor(x => x.GameId);
    }

    [Fact]
    public void NegativeAttempts_HasError()
    {
        var result = validator.TestValidate(Valid() with { Attempts = -1 });

        result.ShouldHaveValidationErrorFor(x => x.Attempts);
    }

    private static RecordGameResultRequest Valid() => new(
        GameId: "meltho",
        PlayedOn: new DateOnly(2026, 6, 7),
        Solved: true,
        Attempts: 3,
        DetailJson: null);
}
