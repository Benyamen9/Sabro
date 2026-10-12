namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// What the answer form may know about one melody's modes, once the player has
/// named it.
/// </summary>
/// <remarks>
/// Only melodies with something to say are listed: one that is sung in no mode at
/// all (a madrosho), or one with a published chant in a conditional mode (see
/// <see cref="ChantAnswerOptionsDto.ConditionalModeIds"/>). A melody absent from the
/// list is asked the always-offered modes and nothing else.
/// </remarks>
/// <param name="Melody">The melody name, exactly as in <see cref="ChantAnswerOptionsDto.Melodies"/>.</param>
/// <param name="HasModes">
/// False when no published chant of this melody carries a mode: the round then has
/// no mode to ask.
/// </param>
/// <param name="ConditionalModeIds">
/// The conditional modes this melody has a published chant in — never the
/// always-offered ones, which would be the forbidden melody-to-mode join.
/// </param>
public sealed record MelodyModeHintDto(
    string Melody,
    bool HasModes,
    IReadOnlyList<Guid> ConditionalModeIds);
