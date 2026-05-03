using Sabro.Translations.Domain;

namespace Sabro.UnitTests.Translations.Domain;

public class TextVersionTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsSuccessfulResultCarryingTheEntity()
    {
        var result = TextVersion.Create(code: "en", name: "English", isRightToLeft: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Code.Should().Be("en");
        result.Value.Name.Should().Be("English");
        result.Value.IsRightToLeft.Should().BeFalse();
        result.Value.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingCode_ReturnsValidationFailure(string? code)
    {
        var result = TextVersion.Create(code: code!, name: "English", isRightToLeft: false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithMissingName_ReturnsValidationFailure(string? name)
    {
        var result = TextVersion.Create(code: "en", name: name!, isRightToLeft: false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("validation");
    }

    [Theory]
    [InlineData("e")]
    [InlineData("english")]
    [InlineData("e1")]
    [InlineData("e-n")]
    public void Create_WithCodeOfInvalidFormat_ReturnsValidationFailure(string code)
    {
        var result = TextVersion.Create(code: code, name: "English", isRightToLeft: false);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("validation");
    }

    [Fact]
    public void Create_WithUppercaseCode_NormalizesToLowercase()
    {
        var result = TextVersion.Create(code: "EN", name: "English", isRightToLeft: false);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("en");
    }

    [Fact]
    public void Create_WithSurroundingWhitespace_TrimsCodeAndName()
    {
        var result = TextVersion.Create(code: "  en  ", name: "  English  ", isRightToLeft: false);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("en");
        result.Value.Name.Should().Be("English");
    }

    [Fact]
    public void Create_WithIsActiveTrue_PersistsTheFlag()
    {
        var result = TextVersion.Create(code: "en", name: "English", isRightToLeft: false, isActive: true);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsActive.Should().BeTrue();
    }
}
