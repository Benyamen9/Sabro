namespace Sabro.BethGazo.Public;

/// <summary>
/// The projection of a chant that Nahlo needs to render a round: the recording to
/// play, and the three parts of the answer.
/// </summary>
/// <param name="ModeName">
/// The mode's name rather than its id — the Play module has no business resolving
/// a foreign key into another module's reference table, and the client shows the
/// name.
/// </param>
public sealed record PlayableChant(
    Guid Id,
    string SyriacIncipit,
    string? SyriacIncipitVocalized,
    string Transliteration,
    string ModeName,
    string? Shuhlofo,
    string AudioUrl);
