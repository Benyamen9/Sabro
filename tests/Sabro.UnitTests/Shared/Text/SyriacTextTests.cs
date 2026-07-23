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

    [Fact]
    public void IsSyriacOnly_WithPlainSyriacWord_ReturnsTrue()
    {
        // ܟܬܒ — three base Syriac letters, no marks.
        SyriacText.IsSyriacOnly("ܟܬܒ").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithVocalizationMarks_ReturnsTrue()
    {
        // ܟܬ݂ܳܒ݂ — base letters plus combining rukkakha/zqapha marks in the Syriac block.
        SyriacText.IsSyriacOnly("ܟܬ݂ܳܒ݂").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithSeyame_ReturnsTrue()
    {
        // ܡܝ̈ܐ — mayo "water", with seyame (U+0308 COMBINING DIAERESIS) over the Yodh.
        SyriacText.IsSyriacOnly("ܡܝ̈ܐ").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithWhitespace_IsIgnoredAndReturnsTrue()
    {
        SyriacText.IsSyriacOnly("ܒܪ ܐܢܫܐ").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithLineaOccultans_ReturnsTrue()
    {
        // ܐ̱ܚܪܺܝܢ — "other", with the linea occultans (U+0331 COMBINING MACRON BELOW)
        // marking the silenced ʾolaph.
        SyriacText.IsSyriacOnly("ܐ̱ܚܪܺܝܢ").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithCompoundIdiomHyphen_ReturnsTrue()
    {
        // ܐܰܚܺܝܕ݂-ܟ݁ܽܠ — "Lord of all" (SEDRA), a two-word idiom joined by a hyphen
        // in its vocalized spelling.
        SyriacText.IsSyriacOnly("ܐܰܚܺܝܕ݂-ܟ݁ܽܠ").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithGenericQushoyoRukkokhoDots_ReturnsTrue()
    {
        // ܐܰܓ̇ܬ̣ܳܐ — SEDRA form encoding qushoyo (U+0307 COMBINING DOT ABOVE) and
        // rukkokho (U+0323 COMBINING DOT BELOW) as generic Unicode marks rather than
        // the dedicated Syriac-block marks.
        SyriacText.IsSyriacOnly("ܐܰܓ̇ܬ̣ܳܐ").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithZeroWidthJoiner_ReturnsTrue()
    {
        // ܡܚܰܠ‍ܠܳܢܳܐ — SEDRA form with a zero width joiner (U+200D) between the two
        // halves of the word.
        SyriacText.IsSyriacOnly("ܡܚܰܠ‍ܠܳܢܳܐ").Should().BeTrue();
    }

    [Fact]
    public void IsSyriacOnly_WithMacronAbove_ReturnsTrue()
    {
        // ܐ̄ܘ — the linea occultans placed above the letter (U+0304 COMBINING MACRON)
        // instead of below, a SEDRA variant seen on word-final letters.
        SyriacText.IsSyriacOnly("ܐ̄ܘ").Should().BeTrue();
    }

    [Theory]
    [InlineData("mayo")]
    [InlineData("ܟܬܒa")]
    [InlineData("café")]
    public void IsSyriacOnly_WithNonSyriacCharacters_ReturnsFalse(string input)
    {
        SyriacText.IsSyriacOnly(input).Should().BeFalse();
    }
}
