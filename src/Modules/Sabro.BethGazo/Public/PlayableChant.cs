using Sabro.BethGazo.Domain;

namespace Sabro.BethGazo.Public;

/// <summary>
/// The projection of a chant that Nahlo needs to render a round: the recording to
/// play, and the parts of the answer.
/// </summary>
/// <param name="Id">The chant's id.</param>
/// <param name="SyriacIncipit">The chant's opening words in Syriac; null until they are read from the book.</param>
/// <param name="SyriacIncipitVocalized">The same incipit with vowel points; optional, and set only with the plain one.</param>
/// <param name="Transliteration">The melody name, romanized. Its own datum: never derived from the incipit.</param>
/// <param name="SectionName">
/// The section the chant belongs to — an answer part in its own right, and the
/// thing that decides whether <paramref name="ModeName"/> exists.
/// </param>
/// <param name="ModeName">
/// The mode's name rather than its id — the Play module has no business resolving
/// a foreign key into another module's reference table, and the client shows the
/// name. <b>Null when the section has no modes</b> (the madroshe), in which case
/// the round has no mode to score.
/// </param>
public sealed record PlayableChant(
    Guid Id,
    string? SyriacIncipit,
    string? SyriacIncipitVocalized,
    string Transliteration,
    string SectionName,
    string? ModeName,
    ChantVariantKind VariantKind,
    int? VariantNumber,
    string AudioUrl);
