using Sabro.Historical.Domain;

namespace Sabro.Historical.Public;

/// <summary>
/// Flat, read-only projection of a figure as needed to play and reveal a Shmo puzzle.
/// Shmo scores guesses client-side, so every scored attribute travels with the answer.
/// </summary>
public sealed record PlayableHistoricalFigure(
    Guid Id,
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalPeriod Period,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureTradition? Tradition,
    HistoricalFigureGender Gender);
