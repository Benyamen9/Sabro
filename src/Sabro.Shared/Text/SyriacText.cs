using System.Text;

namespace Sabro.Shared.Text;

/// <summary>
/// Unicode helpers for Syriac script per CLAUDE.md rules: NFC normalization
/// before storage, validation against the Syriac (U+0700–U+074F) and Syriac
/// Supplement (U+0860–U+086F) ranges, plus a small set of standard marks and
/// separators (seyame, the linea occultans, and a compound-word hyphen) that
/// have no dedicated Syriac-block code point of their own.
/// </summary>
public static class SyriacText
{
    private const int SyriacStart = 0x0700;
    private const int SyriacEnd = 0x074F;
    private const int SyriacSupplementStart = 0x0860;
    private const int SyriacSupplementEnd = 0x086F;

    /// <summary>
    /// COMBINING DIAERESIS — the Unicode-standard encoding for seyame (the two dots
    /// marking an inherently-plural noun, e.g. mayo "water"). It has no dedicated
    /// code point in the Syriac block itself, so it must be allowed explicitly.
    /// </summary>
    private const int Seyame = 0x0308;

    /// <summary>
    /// COMBINING MACRON BELOW — the Unicode-standard encoding for the linea occultans,
    /// the line marking a letter that is written but not pronounced (a quiescent
    /// consonant, e.g. the silenced ʾolaph in ܐ̱ܚܪܺܝܢ "other"). Like seyame, it has no
    /// dedicated Syriac-block code point, so it must be allowed explicitly.
    /// </summary>
    private const int LineaOccultans = 0x0331;

    /// <summary>
    /// HYPHEN-MINUS — lexicography joins the two halves of a compound idiom with a
    /// plain hyphen in the vocalized spelling (e.g. ܐܰܚܺܝܕ݂-ܟ݁ܽܠ, "Lord of all", from
    /// SEDRA). Treated like whitespace: a separator between words, not phonetic content.
    /// </summary>
    private const int HyphenMinus = 0x002D;

    /// <summary>Normalizes input to NFC. Always call this before persisting Syriac text.</summary>
    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.IsNormalized(NormalizationForm.FormC)
            ? input
            : input.Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// True when every non-whitespace, non-hyphen code point falls in the Syriac or
    /// Syriac Supplement block, or is one of the two standard combining-mark exceptions
    /// with no Syriac-block code point of their own: seyame (<see cref="Seyame"/>) and
    /// the linea occultans (<see cref="LineaOccultans"/>).
    /// </summary>
    public static bool IsSyriacOnly(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        foreach (var rune in input.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune) || rune.Value == HyphenMinus)
            {
                continue;
            }

            var value = rune.Value;
            var inSyriac = value >= SyriacStart && value <= SyriacEnd;
            var inSupplement = value >= SyriacSupplementStart && value <= SyriacSupplementEnd;

            if (!inSyriac && !inSupplement && value != Seyame && value != LineaOccultans)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Counts the base letters in <paramref name="input"/>: Unicode letter-category
    /// code points only. Combining marks (vowel points, seyame, diacritics) are not
    /// counted. Intended for already-validated Syriac text; used to derive the Meltho
    /// playable length from the unvocalized form, so it is independent of vocalization.
    /// </summary>
    public static int CountLetters(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var count = 0;
        foreach (var rune in input.EnumerateRunes())
        {
            if (Rune.IsLetter(rune))
            {
                count++;
            }
        }

        return count;
    }
}
