namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// A mode, for the backoffice's mode picker and eventually the game's own.
/// </summary>
/// <remarks>
/// Served as a list rather than baked into the client as a constant, because the
/// set grows: the owner adds modes as he works through the tradition, and some
/// sets run past eight. A hardcoded client list would silently omit them.
/// <para>
/// <see cref="SyriacName"/> and <see cref="Descriptions"/> are filled for the
/// backoffice and the hub's Beth Gazo; Nahlo's answer options leave them null.
/// </para>
/// </remarks>
public sealed record BethGazoModeDto(
    Guid Id,
    string Name,
    int Position,
    string? SyriacName = null,
    IReadOnlyList<BethGazoDescriptionDto>? Descriptions = null);
