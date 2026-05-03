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
}
