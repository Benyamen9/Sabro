using System.Text;

namespace Sabro.Shared.Text;

/// <summary>
/// Unicode helpers for Syriac script per CLAUDE.md rules: NFC normalization
/// before storage, validation against the Syriac (U+0700–U+074F) and Syriac
/// Supplement (U+0860–U+086F) ranges.
/// </summary>
public static class SyriacText
{
    private const int SyriacStart = 0x0700;
    private const int SyriacEnd = 0x074F;
    private const int SyriacSupplementStart = 0x0860;
    private const int SyriacSupplementEnd = 0x086F;

    /// <summary>Normalizes input to NFC. Always call this before persisting Syriac text.</summary>
    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.IsNormalized(NormalizationForm.FormC)
            ? input
            : input.Normalize(NormalizationForm.FormC);
    }

    /// <summary>True when every non-whitespace code point falls in the Syriac or Syriac Supplement block.</summary>
    public static bool IsSyriacOnly(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        foreach (var rune in input.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                continue;
            }

            var value = rune.Value;
            var inSyriac = value >= SyriacStart && value <= SyriacEnd;
            var inSupplement = value >= SyriacSupplementStart && value <= SyriacSupplementEnd;

            if (!inSyriac && !inSupplement)
            {
                return false;
            }
        }

        return true;
    }
}
