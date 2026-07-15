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
    /// <summary>Combining mawotho mark (U+0307, dot above): multiplies its letter by 10.</summary>
    public const char Mawotho = '̇';

    /// <summary>Combining alfayo mark (U+0748, Syriac oblique line below): multiplies its letter by 1000.</summary>
    public const char Alfayo = '݈';

    /// <summary>Combining hsiroyuth alfayo mark (U+0331, line below): multiplies its letter by 10,000.</summary>
    public const char HsiroyuthAlfayo = '̱';

    /// <summary>Combining mawoth alfayo mark (U+032D, circumflex below): multiplies its letter by 100,000.</summary>
    public const char MawothAlfayo = '̭';

    public const int MinValue = 1;

    public const int MaxValue = 999_999;

    /// <summary>Every multiplier mark a stored tile form may carry (one tile = letter + optional mark).</summary>
    public static readonly IReadOnlySet<char> Marks = new HashSet<char> { Mawotho, Alfayo, HsiroyuthAlfayo, MawothAlfayo };

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
    /// Spells a value in the compact marked form the Extreme ladder uses: one
    /// tile per non-zero decimal digit, each rank carried by its mark —
    /// hundreds 500–900 on a tens letter with mawotho (600 = ܣ̇), thousands on
    /// a unit letter with alfayo, ten-thousands with hsiroyuth alfayo, and
    /// hundred-thousands with mawoth alfayo. Every form this emits is a valid
    /// spelling under the client's guess grammar (per-scale symbols, scales
    /// strictly descending); it is deliberately NOT the canonical form — the
    /// point of Extreme is meeting all four marks.
    /// </summary>
    public static string SpellMarked(int value)
    {
        if (value is < MinValue or > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Spellable values are {MinValue}..{MaxValue}.");
        }

        var builder = new StringBuilder();

        AppendDigit(builder, (value / 100_000) % 10, Units, MawothAlfayo);
        AppendDigit(builder, (value / 10_000) % 10, Units, HsiroyuthAlfayo);
        AppendDigit(builder, (value / 1_000) % 10, Units, Alfayo);

        var hundreds = (value / 100) % 10;
        if (hundreds is >= 1 and <= 4)
        {
            builder.Append(hundreds switch { 1 => 'ܩ', 2 => 'ܪ', 3 => 'ܫ', _ => 'ܬ' });
        }
        else if (hundreds >= 5)
        {
            // 500–900 in one tile: the tens letter of the digit, ×10 by mawotho.
            builder.Append(Tens[hundreds - 1]).Append(Mawotho);
        }

        AppendDigit(builder, (value / 10) % 10, Tens, mark: null);
        AppendDigit(builder, value % 10, Units, mark: null);

        return builder.ToString();
    }

    /// <summary>
    /// The number of tiles the canonical spelling occupies on the Mno board:
    /// base letters only — a letter and its mark are one symbol, one tile.
    /// </summary>
    public static int TileCount(int value)
    {
        return TileCountOf(Spell(value));
    }

    /// <summary>Board width of any tile form: every character is a tile except the combining marks.</summary>
    public static int TileCountOf(string tileForm)
    {
        var count = 0;
        foreach (var ch in tileForm)
        {
            if (!Marks.Contains(ch))
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

    private static void AppendDigit(StringBuilder builder, int digit, string[] letters, char? mark)
    {
        if (digit == 0)
        {
            return;
        }

        builder.Append(letters[digit - 1]);
        if (mark is not null)
        {
            builder.Append(mark.Value);
        }
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
