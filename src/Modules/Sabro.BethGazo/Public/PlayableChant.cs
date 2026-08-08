namespace Sabro.BethGazo.Public;

/// <summary>
/// The projection of a chant that Nahlo needs to render a round: the recording to
/// play, and the parts of the answer.
/// </summary>
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
    string SyriacIncipit,
    string? SyriacIncipitVocalized,
    string Transliteration,
    string SectionName,
    string? ModeName,
    int? ShuhlofoNumber,
    string AudioUrl);
