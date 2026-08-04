namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A mode, for the backoffice's mode picker and eventually the game's own.
/// </summary>
/// <remarks>
/// Served as a list rather than baked into the client as a constant, because the
/// set grows: the owner adds modes as he works through the tradition, and some
/// sets run past eight. A hardcoded client list would silently omit them.
/// </remarks>
public sealed record BethGazoModeDto(Guid Id, string Name, int Position);
