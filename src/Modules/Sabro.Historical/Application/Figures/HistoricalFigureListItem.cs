using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

/// <summary>
/// Public roster projection: one published figure with the attributes Shmo scores
/// guesses against. Deliberately carries no editorial state (<c>Status</c>) and no
/// puzzle-pool marker (<c>PlayableInShmo</c>) — the roster is served anonymously and
/// must not let clients enumerate future Shmo answers.
/// </summary>
public sealed record HistoricalFigureListItem(
    Guid Id,
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureTradition? Tradition,
    HistoricalFigureGender Gender);
