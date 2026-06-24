namespace Sabro.Shared.Text;

/// <summary>
/// One letter of a decomposed Syriac word: its base glyph, identity, vowel, and — for
/// begadkephat letters — its hardening and where that hardening was determined.
/// </summary>
/// <param name="Letter">The base letter glyph alone (combining marks excluded).</param>
/// <param name="Code">The stable letter identifier.</param>
/// <param name="Vowel">The vowel sign on this letter, or null.</param>
/// <param name="IsBegadkephat">True for the six begadkephat letters (ܒ ܓ ܕ ܟ ܦ ܬ).</param>
/// <param name="Hardening">Qushoyo/Rukkokho for begadkephat letters; null otherwise.</param>
/// <param name="HardeningSource">Whether the hardening was marked, computed, or N/A.</param>
public sealed record SyriacLetter(
    string Letter,
    SyriacLetterCode Code,
    SyriacVowel? Vowel,
    bool IsBegadkephat,
    LetterHardening? Hardening,
    HardeningSource HardeningSource);
