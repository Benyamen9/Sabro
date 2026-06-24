using System.Globalization;
using System.Text;

namespace Sabro.Shared.Text;

/// <summary>
/// Decomposes a vocalized Syriac word into its ordered letters, reading the vowel and (for
/// begadkephat letters) the qushoyo/rukkokho hardening. Hardening is read from explicit
/// qushshaya (U+0741) / rukkakha (U+0742) marks when present; otherwise it is a tentative
/// first-pass from the post-vocalic spirantization heuristic (soft after a vowel, hard
/// otherwise), tagged <see cref="HardeningSource.Computed"/> so callers can show it as
/// unverified. Pure and stateless — the source of truth is the vocalized form itself.
/// </summary>
public static class SyriacComposition
{
    private const int SyriacStart = 0x0700;
    private const int SyriacEnd = 0x074F;
    private const int SyriacSupplementStart = 0x0860;
    private const int SyriacSupplementEnd = 0x086F;

    private const int Qushshaya = 0x0741;
    private const int Rukkakha = 0x0742;

    private static readonly HashSet<int> Begadkephat = new()
    {
        0x0712, // Beth
        0x0713, // Gamal
        0x0715, // Dalath
        0x071F, // Kaph
        0x0726, // Pe
        0x072C, // Taw
    };

    /// <summary>
    /// Returns the ordered letters of <paramref name="vocalized"/>. An empty, whitespace, or
    /// null input yields an empty list. Input is NFC-normalized internally.
    /// </summary>
    public static IReadOnlyList<SyriacLetter> Decompose(string? vocalized)
    {
        if (string.IsNullOrWhiteSpace(vocalized))
        {
            return Array.Empty<SyriacLetter>();
        }

        var text = SyriacText.Normalize(vocalized);

        var units = Segment(text);

        var result = new List<SyriacLetter>(units.Count);
        SyriacVowel? previousVowel = null;
        var isFirst = true;
        foreach (var unit in units)
        {
            var isBegadkephat = Begadkephat.Contains(unit.Base.Value);
            var vowel = MapVowel(unit.Marks);
            var (hardening, source) = ResolveHardening(unit, isBegadkephat, isFirst, previousVowel);

            result.Add(new SyriacLetter(
                unit.Base.ToString(),
                MapLetter(unit.Base.Value),
                vowel,
                isBegadkephat,
                hardening,
                source));

            previousVowel = vowel;
            isFirst = false;
        }

        return result;
    }

    private static List<RawUnit> Segment(string text)
    {
        var units = new List<RawUnit>();
        RawUnit? current = null;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                continue;
            }

            if (IsBaseLetter(rune))
            {
                current = new RawUnit(rune);
                units.Add(current);
            }
            else if (current is not null && Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
            {
                current.Marks.Add(rune.Value);
            }

            // A combining mark before any base letter, or any other code point, is ignored.
        }

        return units;
    }

    private static (LetterHardening? Hardening, HardeningSource Source) ResolveHardening(
        RawUnit unit,
        bool isBegadkephat,
        bool isFirst,
        SyriacVowel? previousVowel)
    {
        if (!isBegadkephat)
        {
            return (null, HardeningSource.None);
        }

        if (unit.Marks.Contains(Qushshaya))
        {
            return (LetterHardening.Qushoyo, HardeningSource.Marked);
        }

        if (unit.Marks.Contains(Rukkakha))
        {
            return (LetterHardening.Rukkokho, HardeningSource.Marked);
        }

        // Post-vocalic spirantization: soft when immediately preceded by a vowel, hard at the
        // start of the word or after a vowelless consonant. A tentative first pass.
        var hardening = !isFirst && previousVowel is not null
            ? LetterHardening.Rukkokho
            : LetterHardening.Qushoyo;
        return (hardening, HardeningSource.Computed);
    }

    private static bool IsBaseLetter(Rune rune)
    {
        if (!Rune.IsLetter(rune))
        {
            return false;
        }

        var value = rune.Value;
        return (value >= SyriacStart && value <= SyriacEnd)
            || (value >= SyriacSupplementStart && value <= SyriacSupplementEnd);
    }

    private static SyriacVowel? MapVowel(IReadOnlyList<int> marks)
    {
        foreach (var mark in marks)
        {
            switch (mark)
            {
                case 0x0730 or 0x0731 or 0x0732:
                    return SyriacVowel.Pthaha;
                case 0x0733 or 0x0734 or 0x0735:
                    return SyriacVowel.Zqapha;
                case 0x0736 or 0x0737:
                    return SyriacVowel.Rbasa;
                case 0x0738 or 0x0739:
                    return SyriacVowel.Zlama;
                case 0x073A or 0x073B or 0x073C:
                    return SyriacVowel.Hbasa;
                case 0x073D or 0x073E:
                    return SyriacVowel.Esasa;
                case 0x073F:
                    return SyriacVowel.Rwaha;
            }
        }

        return null;
    }

    private static SyriacLetterCode MapLetter(int value) => value switch
    {
        0x0710 => SyriacLetterCode.Alaph,
        0x0712 => SyriacLetterCode.Beth,
        0x0713 or 0x0714 => SyriacLetterCode.Gamal,
        0x0715 or 0x0716 => SyriacLetterCode.Dalath,
        0x0717 => SyriacLetterCode.He,
        0x0718 => SyriacLetterCode.Waw,
        0x0719 => SyriacLetterCode.Zayn,
        0x071A => SyriacLetterCode.Heth,
        0x071B or 0x071C => SyriacLetterCode.Teth,
        0x071D or 0x071E => SyriacLetterCode.Yudh,
        0x071F => SyriacLetterCode.Kaph,
        0x0720 => SyriacLetterCode.Lamadh,
        0x0721 => SyriacLetterCode.Mim,
        0x0722 => SyriacLetterCode.Nun,
        0x0723 or 0x0724 => SyriacLetterCode.Semkath,
        0x0725 => SyriacLetterCode.Ayn,
        0x0726 or 0x0727 => SyriacLetterCode.Pe,
        0x0728 => SyriacLetterCode.Sadhe,
        0x0729 => SyriacLetterCode.Qaph,
        0x072A => SyriacLetterCode.Rish,
        0x072B => SyriacLetterCode.Shin,
        0x072C => SyriacLetterCode.Taw,
        _ => SyriacLetterCode.Unknown,
    };

    private sealed class RawUnit
    {
        public RawUnit(Rune baseRune) => Base = baseRune;

        public Rune Base { get; }

        public List<int> Marks { get; } = new();
    }
}
