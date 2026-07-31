using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

public sealed record CreateHistoricalFigureRequest(
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalPeriod Period,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureGender Gender,
    HistoricalFigureTradition? Tradition = null,
    IReadOnlyList<HistoricalFigureDescriptionRequest>? Descriptions = null);
