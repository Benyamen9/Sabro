namespace Sabro.Historical.Application.Figures;

/// <summary>Body for the playable-toggle endpoint. Marking playable requires a published figure.</summary>
public sealed record SetPlayableHistoricalFigureRequest(bool Playable);
