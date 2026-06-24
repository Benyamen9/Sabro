namespace Sabro.Shared.Text;

/// <summary>
/// A Syriac vowel sign, grouped across its above/below/dotted Unicode variants. This is a
/// first-pass classification a Syriacist may refine; the names are stable identifiers the
/// frontend localizes.
/// </summary>
public enum SyriacVowel
{
    /// <summary>Pthaha (a) — U+0730–U+0732.</summary>
    Pthaha,

    /// <summary>Zqapha (o/ā) — U+0733–U+0735.</summary>
    Zqapha,

    /// <summary>Rbasa (e) — U+0736–U+0737.</summary>
    Rbasa,

    /// <summary>Zlama (e/ē) — U+0738–U+0739.</summary>
    Zlama,

    /// <summary>Hbasa (i) — U+073A–U+073C.</summary>
    Hbasa,

    /// <summary>Esasa (u) — U+073D–U+073E.</summary>
    Esasa,

    /// <summary>Rwaha — U+073F.</summary>
    Rwaha,
}
