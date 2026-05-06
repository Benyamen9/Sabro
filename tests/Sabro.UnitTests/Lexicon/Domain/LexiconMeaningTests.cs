using Sabro.Lexicon.Domain;

namespace Sabro.UnitTests.Lexicon.Domain;

public class LexiconMeaningTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsSuccess()
    {
        var result = LexiconMeaning.Create("en", "to write");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Language.Should().Be("en");
        result.Value.Text.Should().Be("to write");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingLanguage_ReturnsValidationFailure(string? language)
    {
        var result = LexiconMeaning.Create(language!, "to write");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingText_ReturnsValidationFailure(string? text)
    {
        var result = LexiconMeaning.Create("en", text!);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_LowercasesLanguageCode()
    {
        var result = LexiconMeaning.Create("EN", "to write");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Language.Should().Be("en");
    }

    [Fact]
    public void Create_TrimsLanguageAndText()
    {
        var result = LexiconMeaning.Create("  en  ", "  to write  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Language.Should().Be("en");
        result.Value.Text.Should().Be("to write");
    }

    [Theory]
    [InlineData("e")]
    [InlineData("english")]
    [InlineData("12")]
    [InlineData("e!")]
    public void Create_WithMalformedLanguageCode_ReturnsValidationFailure(string language)
    {
        var result = LexiconMeaning.Create(language, "to write");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }
}
