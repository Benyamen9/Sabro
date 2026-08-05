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
/// <param name="ModeName">The mode's name, not its id: the client shows the name, and the mode
/// list is a reference table Play has no business resolving.</param>
/// <param name="Shuhlofo">Null when this melody has no variation — which is most of them. A
/// round's third answer part is then simply absent rather than empty.</param>
public sealed record NahloPuzzleDto(
    DateOnly Date,
    Guid ChantId,
    string AudioUrl,
    string Transliteration,
    string ModeName,
    string? Shuhlofo,
    string SyriacIncipit,
    string? SyriacIncipitVocalized);
