using System.Text;

namespace Sabro.Play.Domain;

/// <summary>
/// Canonical speller for Syriac alphabetic numerals, per the Mno numeral rules:
/// additive spelling, largest value first, at most one letter per rank below
/// 500, compound hundreds from 500 (500 = ܬܩ … 900 = ܬܬܩ), and from 1000 up
/// the thousands count spelled the same way with the alfayo mark (oblique line
/// below) on each thousands letter. The canonical form is the unmarked additive
/// spelling wherever the value allows it; marks appear only where unavoidable.
/// Guess-side parsing of the many valid alternative spellings is client logic
/// (Mno), not Sabro's — this speller exists to produce the daily solution's
/// stored tile form and to size equations in tiles.
/// </summary>
public static class SyriacNumerals
{
    /// <summary>Combining alfayo mark (U+0748, Syriac oblique line below): multiplies its letter by 1000.</summary>
    public const char Alfayo = '݈';

    public const int MinValue = 1;

    public const int MaxValue = 999_999;

    private static readonly string[] Units = ["ܐ", "ܒ", "ܓ", "ܕ", "ܗ", "ܘ", "ܙ", "ܚ", "ܛ"];
    private static readonly string[] Tens = ["ܝ", "ܟ", "ܠ", "ܡ", "ܢ", "ܣ", "ܥ", "ܦ", "ܨ"];

    /// <summary>Spells a value in canonical form. Throws for values outside [1, 999999].</summary>
    public static string Spell(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Spellable values are {MinValue}..{MaxValue}.");
        }

        var builder = new StringBuilder();
        var thousands = value / 1000;
        if (thousands > 0)
        {
            foreach (var letter in LettersUnderThousand(thousands))
            {
                builder.Append(letter).Append(Alfayo);
            }
        }

        foreach (var letter in LettersUnderThousand(value % 1000))
        {
            builder.Append(letter);
        }

        return builder.ToString();
    }

    /// <summary>
    /// The number of tiles the canonical spelling occupies on the Mno board:
    /// base letters only — a letter and its mark are one symbol, one tile.
    /// </summary>
    public static int TileCount(int value)
    {
        var spelled = Spell(value);
        var count = 0;
        foreach (var ch in spelled)
        {
            if (ch != Alfayo)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The unmarked value of a single numeral letter. Throws for non-numeral characters.</summary>
    public static int ValueOf(char letter)
    {
        var index = Array.IndexOf(Units, letter.ToString());
        if (index >= 0)
        {
            return index + 1;
        }

        index = Array.IndexOf(Tens, letter.ToString());
        if (index >= 0)
        {
            return (index + 1) * 10;
        }

        return letter switch
        {
            'ܩ' => 100,
            'ܪ' => 200,
            'ܫ' => 300,
            'ܬ' => 400,
            _ => throw new ArgumentOutOfRangeException(nameof(letter), letter, "Not a Syriac numeral letter."),
        };
    }

    private static IEnumerable<string> LettersUnderThousand(int value)
    {
        var hundreds = value / 100;

        // Compound hundreds: 400 is the largest hundreds letter (taw), so
        // 500-800 are taw + one more, and 900 is taw + taw + qof.
        while (hundreds >= 4)
        {
            yield return "ܬ";
            hundreds -= 4;
        }

        if (hundreds > 0)
        {
            yield return hundreds switch
            {
                1 => "ܩ",
                2 => "ܪ",
                _ => "ܫ",
            };
        }

        var tens = (value % 100) / 10;
        if (tens > 0)
        {
            yield return Tens[tens - 1];
        }

        var units = value % 10;
        if (units > 0)
        {
            yield return Units[units - 1];
        }
    }
}
