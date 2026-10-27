using System.Text;
using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Public;

namespace Sabro.Play.Application.Nahlo;

/// <summary>
/// Whether a guess names the day's chant: the same rule Nahlo scores by
/// (<c>app/utils/compare.ts</c>), repeated here so Sabro can check a claimed win.
/// </summary>
/// <remarks>
/// Melody and mode match when equal after NFC, trimming and lower-casing; there is
/// no tolerance for other spellings. The mode is asked only when the chant has one
/// (the madroshe have none). The third part is "is this chant a shuḥlofo?", which
/// a ḥrino answers "no" exactly as the melody's own form does.
/// Keep in step with Nahlo: if its rule changes, this one must too.
/// </remarks>
internal static class NahloAnswer
{
    public static bool IsRight(NahloGuess guess, PlayableChant chant)
    {
        if (Normalize(guess.Melody) != Normalize(chant.Transliteration))
        {
            return false;
        }

        if (chant.ModeName is not null && Normalize(guess.Mode) != Normalize(chant.ModeName))
        {
            return false;
        }

        return guess.IsShuhlofo == (chant.VariantKind == ChantVariantKind.Shuhlofo);
    }

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Normalize(NormalizationForm.FormC).Trim().ToLowerInvariant();
}
