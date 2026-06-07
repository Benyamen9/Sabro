namespace Sabro.Play.Application.GameResults;

public sealed record GameResultDto(
    Guid Id,
    string LogtoUserId,
    string GameId,
    DateOnly PlayedOn,
    bool Solved,
    int Attempts,
    string? DetailJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
