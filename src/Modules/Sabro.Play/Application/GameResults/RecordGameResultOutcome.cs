namespace Sabro.Play.Application.GameResults;

/// <summary>Outcome of <see cref="IGameResultService.RecordAsync"/> — the result plus whether this call created it.</summary>
public sealed record RecordGameResultOutcome(GameResultDto Result, bool WasCreated);
