using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

/// <summary>
/// Full replacement of a figure's editable fields. Does not carry status or the
/// playable flag — those move through the dedicated publish/unpublish/playable
/// operations. The target figure is identified by the route id, not this body.
/// </summary>
/// <remarks>
/// <see cref="Descriptions"/> is a full replacement like every other field here:
/// omitting it clears the figure's descriptions. Clients that mean to leave them
/// alone must send back the ones they were given.
/// </remarks>
public sealed record UpdateHistoricalFigureRequest(
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalPeriod Period,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureGender Gender,
    HistoricalFigureTradition? Tradition = null,
    IReadOnlyList<HistoricalFigureDescriptionRequest>? Descriptions = null);
