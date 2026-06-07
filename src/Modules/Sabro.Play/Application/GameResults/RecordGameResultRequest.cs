namespace Sabro.Play.Application.GameResults;

/// <summary>
/// The authenticated player's own result for one game on one day. The Logto user
/// id is taken from the validated token, never from the body.
/// </summary>
public sealed record RecordGameResultRequest(
    string GameId,
    DateOnly PlayedOn,
    bool Solved,
    int Attempts,
    string? DetailJson);
