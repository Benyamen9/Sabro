using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

public sealed record HistoricalFigureDto(
    Guid Id,
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalPeriod Period,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureTradition? Tradition,
    HistoricalFigureGender Gender,
    HistoricalFigureStatus Status,
    bool PlayableInShmo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
