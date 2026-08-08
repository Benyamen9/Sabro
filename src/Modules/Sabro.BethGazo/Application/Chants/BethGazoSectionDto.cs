namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A section, for the backoffice's section picker and the game's own.
/// </summary>
/// <remarks>
/// <para>
/// <b>On publishing <see cref="AllowedModeIds"/> to anonymous players.</b> This
/// looks like the kind of join <c>ChantAnswerOptionsDto</c> forbids, and it is
/// worth saying plainly why it is not. That rule exists because pairing a
/// <i>melody</i> with its mode would hand over the answer: the recording already
/// identifies the melody, so a player who recognises what they hear could read the
/// mode off the same row.
/// </para>
/// <para>
/// A section-to-modes map says nothing about any particular chant. It says "the
/// farde use nine modes, the madroshe use none" — a fact about the tradition that
/// a player learns from the game and could read in any printed gazo. It narrows
/// today's answer only once the player has already committed to a section, which
/// is itself a guess they can get wrong. Withholding it would not protect the
/// answer; it would only stop the form from asking a sensible question.
/// </para>
/// </remarks>
/// <param name="AllowedModeIds">
/// <b>Empty is meaningful:</b> the section has no modes, so a chant in it is never
/// asked for one. That is the madroshe.
/// </param>
public sealed record BethGazoSectionDto(
    Guid Id,
    string Name,
    int Position,
    IReadOnlyList<Guid> AllowedModeIds);
