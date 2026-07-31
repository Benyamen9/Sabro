using Sabro.Historical.Domain;

namespace Sabro.UnitTests.Historical.Domain;

public class HistoricalFigureDescriptionTests
{
    [Fact]
    public void Create_WithValidInput_NormalizesLanguageAndTrimsText()
    {
        var result = HistoricalFigureDescription.Create("  EN ", "  A bishop of Edessa.  ");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Language.Should().Be("en");
        result.Value.Text.Should().Be("A bishop of Edessa.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankLanguage_Fails(string language)
    {
        var result = HistoricalFigureDescription.Create(language, "A bishop of Edessa.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("Language");
    }

    [Theory]
    [InlineData("e")]
    [InlineData("engl")]
    [InlineData("e1")]
    [InlineData("en-GB")]
    public void Create_WithMalformedLanguageCode_Fails(string language)
    {
        var result = HistoricalFigureDescription.Create(language, "A bishop of Edessa.");

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("ISO code");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankText_Fails(string text)
    {
        var result = HistoricalFigureDescription.Create("en", text);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("Text");
    }

    [Fact]
    public void Create_AtTheLengthLimit_Succeeds()
    {
        var text = new string('a', HistoricalFigureDescription.MaxTextLength);

        var result = HistoricalFigureDescription.Create("en", text);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().HaveLength(HistoricalFigureDescription.MaxTextLength);
    }

    [Fact]
    public void Create_OneCharacterOverTheLimit_Fails()
    {
        var text = new string('a', HistoricalFigureDescription.MaxTextLength + 1);

        var result = HistoricalFigureDescription.Create("en", text);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain(HistoricalFigureDescription.MaxTextLength.ToString());
    }

    [Fact]
    public void Create_MeasuresLengthAfterTrimming()
    {
        // Surrounding whitespace should not push an otherwise-valid description
        // over the limit — the stored value is what gets measured.
        var text = "  " + new string('a', HistoricalFigureDescription.MaxTextLength) + "  ";

        var result = HistoricalFigureDescription.Create("en", text);

        result.IsSuccess.Should().BeTrue();
    }
}
