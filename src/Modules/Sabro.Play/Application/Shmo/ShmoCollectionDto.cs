namespace Sabro.Play.Application.Shmo;

/// <summary>
/// The signed-in player's figure collection: every figure they have met or found.
/// Figures with no card are the ones still to meet.
/// </summary>
public sealed record ShmoCollectionDto(IReadOnlyList<ShmoCollectionCardDto> Cards);
