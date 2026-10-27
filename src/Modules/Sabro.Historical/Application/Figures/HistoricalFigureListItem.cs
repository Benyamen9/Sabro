using Sabro.Historical.Domain;

namespace Sabro.Historical.Application.Figures;

/// <summary>
/// Public roster projection: one published figure with the attributes Shmo scores
/// guesses against. Deliberately carries no editorial state (<c>Status</c>) and no
/// puzzle-pool marker (<c>PlayableInShmo</c>) — the roster is served anonymously and
/// must not let clients enumerate future Shmo answers.
/// </summary>
/// <remarks>
/// The card fields (emblem, Syriac name, read-more link) and the descriptions are here
/// because the hub draws every card of Beth Ṣalme from this list, and every card
/// carries its story, met or not (Owner, 2026-10-25). They give nothing away about
/// Shmo: the roster was already public, and no description names the day's figure.
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
    IReadOnlyList<HistoricalFigureDescriptionDto> Descriptions,
    HistoricalFigureEmblem? Emblem = null,
    string? SyriacName = null,
    string? ReadMoreUrl = null);
