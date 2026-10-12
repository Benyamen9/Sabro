namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// What a player may answer with.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no shuḥlofo list.</b> There used to be, when the variation was a
/// name a player typed. It is a number now and the round only asks whether the
/// chant is a variation at all, so nothing outside needs the values — and
/// publishing them would say how many variations exist, which is a hint the game
/// never intended to give.
/// </summary>
/// <remarks>
/// <para>
/// <b>The lists are deliberately not joined, and must never become one</b> — with
/// the single, owner-ruled exception of the conditional modes (see
/// <c>ConditionalModeIds</c>). A
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
/// <param name="ConditionalModeIds">
/// <para>
/// The modes the form offers only once the player names a melody that has them.
/// A mode is conditional when some section that has modes does not admit it — the
/// <i>mshaḥelfotho</i>, which belongs to the farde alone. Derived from the section
/// rows rather than named, so no specific mode is hardcoded.
/// </para>
/// <para>
/// <b>The one relaxation of the no-join rule (owner, 2026-10-12).</b> Offering the
/// mshaḥelfotho on every round asks a question almost no melody can answer, so the
/// owner ruled it should appear only when the melody has one. That pairs a melody
/// with a conditional mode, and only that: the always-offered modes are never
/// paired, so naming the melody still leaves the ordinary mode to be answered.
/// </para>
/// </param>
/// <param name="MelodyModeHints">
/// Per melody, whether it has modes at all and which conditional modes it has — see
/// <see cref="MelodyModeHintDto"/>. With the section no longer asked (owner,
/// 2026-10-12), this is what tells the form whether to ask for a mode.
/// </param>
public sealed record ChantAnswerOptionsDto(
    IReadOnlyList<string> Melodies,
    IReadOnlyList<BethGazoSectionDto> Sections,
    IReadOnlyList<BethGazoModeDto> Modes,
    IReadOnlyList<Guid> ConditionalModeIds,
    IReadOnlyList<MelodyModeHintDto> MelodyModeHints);
