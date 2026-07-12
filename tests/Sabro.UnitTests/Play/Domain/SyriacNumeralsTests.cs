using Sabro.Play.Domain;

namespace Sabro.UnitTests.Play.Domain;

public class SyriacNumeralsTests
{
    // Canonical examples confirmed by the owner during the Mno spec session,
    // including the taught mixed compounds 1,234 and 1,500.
    [Theory]
    [InlineData(1, "ܐ")]
    [InlineData(9, "ܛ")]
    [InlineData(10, "ܝ")]
    [InlineData(12, "ܝܒ")]
    [InlineData(15, "ܝܗ")]
    [InlineData(16, "ܝܘ")]
    [InlineData(25, "ܟܗ")]
    [InlineData(90, "ܨ")]
    [InlineData(100, "ܩ")]
    [InlineData(357, "ܫܢܙ")]
    [InlineData(400, "ܬ")]
    [InlineData(499, "ܬܨܛ")]
    [InlineData(500, "ܬܩ")]
    [InlineData(600, "ܬܪ")]
    [InlineData(700, "ܬܫ")]
    [InlineData(800, "ܬܬ")]
    [InlineData(900, "ܬܬܩ")]
    [InlineData(999, "ܬܬܩܨܛ")]
    public void Spell_UnmarkedRange_ProducesCanonicalAdditiveForm(int value, string expected)
    {
        SyriacNumerals.Spell(value).Should().Be(expected);
    }

    // From 1000 up the canonical form marks the thousands letters with alfayo
    // (U+0748, the oblique line below): 1,234 = ܐ+alfayo ܪܠܕ, 1,500 = ܐ+alfayo ܬܩ.
    // The '!' placeholder stands for the combining alfayo mark, substituted below
    // so the expected strings carry the exact codepoint.
    [Theory]
    [InlineData(1000, "ܐ!")]
    [InlineData(1234, "ܐ!ܪܠܕ")]
    [InlineData(1500, "ܐ!ܬܩ")]
    [InlineData(2000, "ܒ!")]
    [InlineData(10000, "ܝ!")]
    [InlineData(25000, "ܟ!ܗ!")]
    [InlineData(100000, "ܩ!")]
    [InlineData(999999, "ܬ!ܬ!ܩ!ܨ!ܛ!ܬܬܩܨܛ")]
    public void Spell_ThousandsRange_MarksThousandsLettersWithAlfayo(int value, string expected)
    {
        SyriacNumerals.Spell(value).Should().Be(expected.Replace('!', SyriacNumerals.Alfayo));
    }

    // A tile is one symbol: a base letter plus its optional mark. Combining
    // marks never count as tiles of their own.
    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 2)]
    [InlineData(357, 3)]
    [InlineData(500, 2)]
    [InlineData(900, 3)]
    [InlineData(999, 5)]
    [InlineData(1000, 1)]
    [InlineData(1234, 4)]
    [InlineData(1500, 3)]
    public void TileCount_CountsBaseLettersOnly(int value, int expected)
    {
        SyriacNumerals.TileCount(value).Should().Be(expected);
    }

    // Round-trip property: summing the letter values back (alfayo = ×1000)
    // recovers the original number for the whole low range.
    [Fact]
    public void Spell_RoundTripsThroughLetterValues()
    {
        for (var value = 1; value <= 9_999; value++)
        {
            var spelled = SyriacNumerals.Spell(value);
            var recovered = 0;
            for (var i = 0; i < spelled.Length; i++)
            {
                var ch = spelled[i];
                if (ch == SyriacNumerals.Alfayo)
                {
                    continue;
                }

                var letterValue = SyriacNumerals.ValueOf(ch);
                var marked = i + 1 < spelled.Length && spelled[i + 1] == SyriacNumerals.Alfayo;
                recovered += marked ? letterValue * 1000 : letterValue;
            }

            recovered.Should().Be(value, because: "the canonical spelling of {0} must sum back to itself", value);
        }
    }

    // Canonical spellings are never ascending: every base letter's effective
    // value (alfayo included) is >= the next one's.
    [Fact]
    public void Spell_ProducesNonAscendingEffectiveValues()
    {
        for (var value = 1; value <= 9_999; value++)
        {
            var values = EffectiveValues(SyriacNumerals.Spell(value));
            values.Should().BeInDescendingOrder(because: "canonical numerals read largest-first ({0})", value);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000)]
    public void Spell_OutOfRange_Throws(int value)
    {
        var act = () => SyriacNumerals.Spell(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static List<int> EffectiveValues(string spelled)
    {
        var values = new List<int>();
        for (var i = 0; i < spelled.Length; i++)
        {
            var ch = spelled[i];
            if (ch == SyriacNumerals.Alfayo)
            {
                continue;
            }

            var marked = i + 1 < spelled.Length && spelled[i + 1] == SyriacNumerals.Alfayo;
            values.Add(marked ? SyriacNumerals.ValueOf(ch) * 1000 : SyriacNumerals.ValueOf(ch));
        }

        return values;
    }
}
