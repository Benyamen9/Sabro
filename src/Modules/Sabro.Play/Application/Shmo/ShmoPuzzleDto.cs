using Sabro.Historical.Domain;

namespace Sabro.Play.Application.Shmo;

/// <summary>
/// Today's Shmo puzzle: the served date plus the answer figure's full attribute
/// set. Shmo scores guesses client-side (same trust model as Meltho and Mno), so
/// every attribute a guess is scored against — and the answer's name, the win
/// condition — travels with the puzzle.
/// </summary>
public sealed record ShmoPuzzleDto(
    DateOnly Date,
    Guid HistoricalFigureId,
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureTradition? Tradition,
    HistoricalFigureGender Gender);
