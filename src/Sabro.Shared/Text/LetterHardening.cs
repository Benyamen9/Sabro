namespace Sabro.Shared.Text;

/// <summary>
/// The hardening state of a begadkephat letter: hard (plosive) or soft (spirantized).
/// Only meaningful for the six begadkephat letters; null elsewhere.
/// </summary>
public enum LetterHardening
{
    /// <summary>Qushoyo — hard/plosive (the dot above, U+0741).</summary>
    Qushoyo,

    /// <summary>Rukkokho — soft/spirantized (the dot below, U+0742).</summary>
    Rukkokho,
}
