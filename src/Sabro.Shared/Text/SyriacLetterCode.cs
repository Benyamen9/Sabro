namespace Sabro.Shared.Text;

/// <summary>
/// Stable, language-neutral identifier for a Syriac base letter. Used by
/// <see cref="SyriacComposition"/> so callers (and the frontend) localize letter names
/// themselves rather than receiving English prose from the backend.
/// </summary>
public enum SyriacLetterCode
{
    /// <summary>A Syriac code point outside the recognized base-letter table.</summary>
    Unknown = 0,
    Alaph,
    Beth,
    Gamal,
    Dalath,
    He,
    Waw,
    Zayn,
    Heth,
    Teth,
    Yudh,
    Kaph,
    Lamadh,
    Mim,
    Nun,
    Semkath,
    Ayn,
    Pe,
    Sadhe,
    Qaph,
    Rish,
    Shin,
    Taw,
}
