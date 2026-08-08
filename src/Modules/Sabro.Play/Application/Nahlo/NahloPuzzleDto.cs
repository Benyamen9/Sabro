using Sabro.BethGazo.Domain;

namespace Sabro.Play.Application.Nahlo;

/// <summary>
/// Today's Nahlo puzzle: the recording to play, and the three parts of the answer
/// a guess is scored against.
/// </summary>
/// <remarks>
/// <para>
/// Nahlo scores guesses client-side, the same trust model as Meltho, Mno and Shmo
/// — every <c>*/today</c> endpoint is anonymous, so a server-side check could not
/// cap attempts anyway. The answer therefore travels with the puzzle.
/// </para>
/// <para>
/// <b>Everything here except <see cref="AudioUrl"/> is the answer.</b> The chant's
/// text identifies it outright — that is the owner's own account of how the modes
/// are told apart — so rendering the incipit before the player has answered hands
/// the round over. It ships because the reveal needs it, not because the round
/// does.
/// </para>
/// </remarks>
/// <param name="SectionName">The section of the treasury this chant belongs to — an answer part,
/// and what decides whether <paramref name="ModeName"/> exists at all.</param>
/// <param name="ModeName">The mode's name, not its id: the client shows the name, and the mode
/// list is a reference table Play has no business resolving. <b>Null when the section has no
/// modes</b> — the madroshe — in which case the round scores three parts, not four. Null here
/// never means "not recorded".</param>
/// <param name="VariantKind">Whether this chant is a <i>shuḥlofo</i> (a variation of the melody),
/// a <i>ḥrino</i> (another chant standing in the same mode), or neither. The round asks only
/// whether it is a shuḥlofo — so a ḥrino scores "no" exactly as a principal chant does — but the
/// two must be distinguished in the data or they collide on the identity key.</param>
/// <param name="VariantNumber">Which one it is (1, 2, 3 …), null exactly when the kind is None.
/// Never asked by the round; it travels so the reveal can name it.</param>
public sealed record NahloPuzzleDto(
    DateOnly Date,
    Guid ChantId,
    string AudioUrl,
    string Transliteration,
    string SectionName,
    string? ModeName,
    ChantVariantKind VariantKind,
    int? VariantNumber,
    string SyriacIncipit,
    string? SyriacIncipitVocalized);
