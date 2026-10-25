namespace Sabro.Play.Application.Shmo;

/// <summary>How far a player has come with a figure in the collection.</summary>
/// <remarks>
/// A figure the player has not met has no card at all: the hub draws those from
/// the public roster. Serialized by name; renaming a value is a breaking
/// <c>/api/v1/</c> change.
/// </remarks>
public enum ShmoCardStatus
{
    /// <summary>Guessed in a Shmo round, or seen as the answer of a round given up.</summary>
    Met,

    /// <summary>Solved as the day's answer.</summary>
    Found,
}
