using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

/// <summary>
/// Full replacement of a figure's editable fields. Does not carry status or the
/// playable flag — those move through the dedicated publish/unpublish/playable
/// operations. The target figure is identified by the route id, not this body.
/// </summary>
public sealed record UpdateHistoricalFigureRequest(
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalPeriod Period,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureGender Gender,
    HistoricalFigureTradition? Tradition = null);
