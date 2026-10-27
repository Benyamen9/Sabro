using Sabro.BethGazo.Domain;

namespace Sabro.UnitTests.BethGazo.Domain;

/// <summary>
/// The Syriac name and one-line descriptions a section or a mode carries for the
/// hub's Beth Gazo. Optional; a full replacement each time.
/// </summary>
public class BethGazoTextTests
{
    [Fact]
    public void Describe_StoresTheSyriacNameAndDescriptions()
    {
        var mode = BethGazoMode.Create("Qadmoyo", 1).Value!;

        var error = mode.Describe("ܩܕܡܝܐ", [Line("en", "The first mode."), Line("fr", "Le premier mode.")]);

        error.Should().BeNull();
        mode.SyriacName.Should().Be("ܩܕܡܝܐ");
        mode.Descriptions.Select(d => d.Language).Should().BeEquivalentTo(["en", "fr"]);
    }

    [Fact]
    public void Describe_WithNothing_ClearsTheText()
    {
        var section = BethGazoSection.Create("Farde", 1).Value!;
        section.Describe("ܦܪ̈ܕܐ", [Line("en", "The odes.")]);

        section.Describe(null, null).Should().BeNull();

        section.SyriacName.Should().BeNull();
        section.Descriptions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Qadmoyo")]
    [InlineData("ܩܕܡܝܐ 1")]
    public void Describe_WithANonSyriacName_IsRejectedAndChangesNothing(string syriacName)
    {
        var mode = BethGazoMode.Create("Qadmoyo", 1).Value!;
        mode.Describe("ܩܕܡܝܐ", null);

        mode.Describe(syriacName, null).Should().NotBeNull();

        mode.SyriacName.Should().Be("ܩܕܡܝܐ");
    }

    [Fact]
    public void Describe_WithTwoLinesInOneLanguage_IsRejected()
    {
        var mode = BethGazoMode.Create("Qadmoyo", 1).Value!;

        mode.Describe(null, [Line("en", "One."), Line("en", "Two.")]).Should().NotBeNull();
        mode.Descriptions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("e", "Text")]
    [InlineData("en", "")]
    [InlineData("english", "Text")]
    public void Description_WithABadLanguageOrNoText_IsRejected(string language, string text)
    {
        BethGazoDescription.Create(language, text).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Description_LongerThanALine_IsRejected()
    {
        BethGazoDescription.Create("en", new string('a', BethGazoDescription.MaxTextLength + 1)).IsSuccess.Should().BeFalse();
    }

    private static BethGazoDescription Line(string language, string text) => BethGazoDescription.Create(language, text).Value!;
}
