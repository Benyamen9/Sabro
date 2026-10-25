namespace Sabro.Play.Application.Shmo;

/// <summary>
/// One recorded Shmo round, as the collection reads it: the result joined with that
/// day's answer. <see cref="AnswerId"/> is null only if the day's puzzle row is
/// missing, which a recorded round should never see.
/// </summary>
internal sealed record ShmoPlayedDay(
    DateOnly Date,
    bool Solved,
    int Attempts,
    IReadOnlyList<Guid> GuessIds,
    Guid? AnswerId);
