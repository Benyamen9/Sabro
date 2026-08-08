namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A mode as the backoffice submits it.
/// </summary>
/// <remarks>
/// No position: a new mode is appended and reordering is a separate move, so an
/// editor never types a number into a uniquely-indexed column. See
/// <see cref="IModeService"/>.
/// </remarks>
public sealed record ModeRequest(string Name);
