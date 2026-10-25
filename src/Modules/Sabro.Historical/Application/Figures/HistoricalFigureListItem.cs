using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

/// <summary>
/// Public roster projection: one published figure with the attributes Shmo scores
/// guesses against. Deliberately carries no editorial state (<c>Status</c>) and no
/// puzzle-pool marker (<c>PlayableInShmo</c>) — the roster is served anonymously and
/// must not let clients enumerate future Shmo answers.
/// </summary>
/// <remarks>
/// The card fields (emblem, Syriac name, read-more link) are here because the hub
/// draws every card from this list, met or not. The description is not: it is the
/// reward for finding a figure, so it travels only with a player's own collection.
/// </remarks>
public sealed record HistoricalFigureListItem(
    Guid Id,
    string Name,
    HistoricalFigureCategory Category,
    int Era,
    HistoricalPeriod Period,
    HistoricalFigureRole Role,
    HistoricalFigureRegion Region,
    HistoricalFigureTradition? Tradition,
    HistoricalFigureGender Gender,
    HistoricalFigureEmblem? Emblem = null,
    string? SyriacName = null,
    string? ReadMoreUrl = null);
