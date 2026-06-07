using System.Text;
using Sabro.Shared.Text;

namespace Sabro.UnitTests.Shared.Text;

public class SyriacTextTests
{
    [Fact]
    public void CountLetters_WithUnvocalizedWord_CountsBaseLetters()
    {
        // ܟܬܒ — three base Syriac letters.
        SyriacText.CountLetters("ܟܬܒ").Should().Be(3);
    }

    [Fact]
    public void CountLetters_WithTwoLetterWord_ReturnsTwo()
    {
        // ܐܒ — Alaph + Beth.
        SyriacText.CountLetters("ܐܒ").Should().Be(2);
    }

    [Fact]
    public void CountLetters_IgnoresVocalizationMarks()
    {
        // ܟܬ݂ܳܒ݂ — three base letters plus combining rukkakha/zqapha marks.
        SyriacText.CountLetters("ܟܬ݂ܳܒ݂").Should().Be(3);
    }

    [Fact]
    public void CountLetters_IsIndependentOfNormalizationForm()
    {
        var nfd = "ܟܬ݂ܳܒ݂".Normalize(NormalizationForm.FormD);

        SyriacText.CountLetters(nfd).Should().Be(3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CountLetters_WithNoLetters_ReturnsZero(string input)
    {
        SyriacText.CountLetters(input).Should().Be(0);
    }
}
