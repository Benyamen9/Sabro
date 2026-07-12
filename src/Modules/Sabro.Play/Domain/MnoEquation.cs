namespace Sabro.Play.Domain;

/// <summary>
/// One generated Mno equation: the neutral ASCII expression (<c>"12*5-8"</c>),
/// the exact Syriac tile form the board shows (the generator's chosen spelling —
/// persisted, never re-derived, because a value can have several valid
/// spellings), and the target the expression evaluates to.
/// </summary>
public sealed record MnoEquation(string Expression, string TileForm, int Target);
