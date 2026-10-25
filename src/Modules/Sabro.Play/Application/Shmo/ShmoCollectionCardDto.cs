namespace Sabro.Play.Application.Shmo;

/// <summary>
/// One figure in the signed-in player's collection.
/// </summary>
/// <param name="HistoricalFigureId">The figure, as listed in the public roster.</param>
/// <param name="Status">Met or found.</param>
/// <param name="TimesMet">Days on which the player guessed the figure or saw it revealed.</param>
/// <param name="FirstFoundOn">The first day the player solved it, when found.</param>
/// <param name="FewestAttempts">The fewest guesses it took, when found.</param>
/// <param name="Illuminated">Whether any find of this figure came out illuminated.</param>
/// <param name="Descriptions">
/// The figure's descriptions, only when found: finding a figure is what reveals
/// its story. Empty for a met card, and for a found figure nobody has written yet.
/// </param>
public sealed record ShmoCollectionCardDto(
    Guid HistoricalFigureId,
    ShmoCardStatus Status,
    int TimesMet,
    DateOnly? FirstFoundOn,
    int? FewestAttempts,
    bool Illuminated,
    IReadOnlyList<ShmoCardDescriptionDto> Descriptions);
