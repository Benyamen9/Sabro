namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// What a player may answer with: the three parts of a Nahlo round, each as its
/// own list.
/// </summary>
/// <remarks>
/// <para>
/// <b>The three lists are deliberately not joined, and must never become one.</b> A
/// public listing that paired each melody with its mode would end the game: the
/// chant's text identifies it outright, so a player who recognises what they are
/// hearing could look the melody up and read the mode off the same row. The mode is
/// the transferable skill Nahlo teaches — it has to be answered, not looked up.
/// That is also why the owner rejected a single combined chant picker: separate
/// fields let someone who knows the mode but not the melody say so.
/// </para>
/// <para>
/// Impossible combinations are allowed rather than blocked, for the same reason.
/// Refusing a melody/mode pair that does not exist would tell the player it does not
/// exist.
/// </para>
/// <para>
/// <b>A suggestion source, not a closed vocabulary.</b> Clients should let a player
/// submit a value that is not in these lists. A chant served as a daily puzzle keeps
/// rendering after it is unpublished (that is deliberate — see
/// <c>IChantPlayablePool</c>), and its melody would by then be absent from here, so
/// a hard select could make the day's own answer unpickable.
/// </para>
/// </remarks>
/// <param name="Melodies">
/// Distinct melody names across published chants, alphabetical. Drawn from published
/// rather than playable chants on purpose: narrowing this to the pool would tell the
/// player the answer is one of these few.
/// </param>
/// <param name="Sections">
/// Every section, in the treasury's order, each carrying the modes it admits.
/// <b>This is not the forbidden join.</b> The rule above forbids pairing a
/// <i>melody</i> with its mode, because the recording already gives the melody away
/// and the pair would then give the mode away with it. A section-to-modes map makes
/// no claim about any chant — "the farde use nine modes, the madroshe use none" is a
/// fact about the tradition, and it only narrows anything after the player has
/// committed to a section, which is itself a guess they can lose. Without it the
/// round cannot know whether to ask for a mode at all.
/// </param>
/// <param name="Modes">
/// Every mode, in traditional order — including any with no published chant yet.
/// Trimming it to modes actually in use would narrow the answer space for free.
/// </param>
/// <param name="Shuhlofe">
/// Distinct variations across published chants, alphabetical. Most melodies have
/// none, so this is the shortest list and often empty.
/// </param>
public sealed record ChantAnswerOptionsDto(
    IReadOnlyList<string> Melodies,
    IReadOnlyList<BethGazoSectionDto> Sections,
    IReadOnlyList<BethGazoModeDto> Modes,
    IReadOnlyList<string> Shuhlofe);
