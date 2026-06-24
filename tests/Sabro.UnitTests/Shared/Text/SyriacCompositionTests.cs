using Sabro.Shared.Text;

namespace Sabro.UnitTests.Shared.Text;

public class SyriacCompositionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Decompose_WithNoContent_ReturnsEmpty(string? input)
    {
        SyriacComposition.Decompose(input).Should().BeEmpty();
    }

    [Fact]
    public void Decompose_SegmentsBaseLettersInOrder()
    {
        // ܟܬܒ — Kaph, Taw, Beth (consonantal, no vocalization).
        var letters = SyriacComposition.Decompose("ܟܬܒ");

        letters.Select(l => l.Code).Should()
            .Equal(SyriacLetterCode.Kaph, SyriacLetterCode.Taw, SyriacLetterCode.Beth);
    }

    [Fact]
    public void Decompose_PreservesTheBaseLetterGlyph()
    {
        var letters = SyriacComposition.Decompose("ܒ");

        letters.Should().ContainSingle();
        letters[0].Letter.Should().Be("ܒ");
    }

    [Fact]
    public void Decompose_ReadsMarkedRukkokhoAndVowel()
    {
        // ܟܬ݂ܳܒ݂ — Kaph, Taw + rukkakha(U+0742) + zqapha(U+0733), Beth + rukkakha.
        var letters = SyriacComposition.Decompose("ܟܬ݂ܳܒ݂");

        letters.Should().HaveCount(3);

        letters[1].Code.Should().Be(SyriacLetterCode.Taw);
        letters[1].Vowel.Should().Be(SyriacVowel.Zqapha);
        letters[1].Hardening.Should().Be(LetterHardening.Rukkokho);
        letters[1].HardeningSource.Should().Be(HardeningSource.Marked);

        letters[2].Code.Should().Be(SyriacLetterCode.Beth);
        letters[2].Hardening.Should().Be(LetterHardening.Rukkokho);
        letters[2].HardeningSource.Should().Be(HardeningSource.Marked);
    }

    [Fact]
    public void Decompose_ReadsMarkedQushoyo()
    {
        // ܒ݁ܐ — Beth + qushshaya(U+0741), Alaph.
        var letters = SyriacComposition.Decompose("ܒ݁ܐ");

        letters[0].Code.Should().Be(SyriacLetterCode.Beth);
        letters[0].Hardening.Should().Be(LetterHardening.Qushoyo);
        letters[0].HardeningSource.Should().Be(HardeningSource.Marked);
    }

    [Fact]
    public void Decompose_NonBegadkephatHasNoHardening()
    {
        // ܫܠܡܐ — Shin, Lamadh, Mim, Alaph (none begadkephat).
        var letters = SyriacComposition.Decompose("ܫܠܡܐ");

        letters.Should().OnlyContain(l => l.IsBegadkephat == false);
        letters.Should().OnlyContain(l => l.Hardening == null);
        letters.Should().OnlyContain(l => l.HardeningSource == HardeningSource.None);
    }

    [Fact]
    public void Decompose_ComputesSoftAfterAVowelWhenUnmarked()
    {
        // ܐܰܒ — Alaph + pthaha(U+0730), Beth (no hardening mark) → soft by heuristic.
        var letters = SyriacComposition.Decompose("ܐܰܒ");

        letters[1].Code.Should().Be(SyriacLetterCode.Beth);
        letters[1].IsBegadkephat.Should().BeTrue();
        letters[1].Hardening.Should().Be(LetterHardening.Rukkokho);
        letters[1].HardeningSource.Should().Be(HardeningSource.Computed);
    }

    [Fact]
    public void Decompose_ComputesHardWordInitialWhenUnmarked()
    {
        // ܒܝܬܐ — Beth at the start of the word → hard by heuristic.
        var letters = SyriacComposition.Decompose("ܒܝܬܐ");

        letters[0].Code.Should().Be(SyriacLetterCode.Beth);
        letters[0].IsBegadkephat.Should().BeTrue();
        letters[0].Hardening.Should().Be(LetterHardening.Qushoyo);
        letters[0].HardeningSource.Should().Be(HardeningSource.Computed);
    }

    [Fact]
    public void Decompose_ComputesHardAfterAVowellessConsonantWhenUnmarked()
    {
        // ܫܒ — Shin (no vowel), Beth → hard (no preceding vowel), computed.
        var letters = SyriacComposition.Decompose("ܫܒ");

        letters[1].Code.Should().Be(SyriacLetterCode.Beth);
        letters[1].Hardening.Should().Be(LetterHardening.Qushoyo);
        letters[1].HardeningSource.Should().Be(HardeningSource.Computed);
    }

    [Theory]
    [InlineData('ܰ', SyriacVowel.Pthaha)]
    [InlineData('ܳ', SyriacVowel.Zqapha)]
    [InlineData('ܶ', SyriacVowel.Rbasa)]
    [InlineData('ܸ', SyriacVowel.Zlama)]
    [InlineData('ܺ', SyriacVowel.Hbasa)]
    [InlineData('ܽ', SyriacVowel.Esasa)]
    [InlineData('ܿ', SyriacVowel.Rwaha)]
    public void Decompose_MapsVowelMarks(char mark, SyriacVowel expected)
    {
        // Alaph carrying the vowel mark under test.
        var letters = SyriacComposition.Decompose("ܐ" + mark);

        letters.Should().ContainSingle();
        letters[0].Vowel.Should().Be(expected);
    }

    [Fact]
    public void Decompose_LetterWithoutAVowel_HasNullVowel()
    {
        var letters = SyriacComposition.Decompose("ܡ");

        letters[0].Vowel.Should().BeNull();
    }
}
