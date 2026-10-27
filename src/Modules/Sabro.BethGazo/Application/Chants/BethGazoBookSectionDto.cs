namespace Sabro.BethGazo.Application.Chants;

/// <summary>
/// One section of the book, with its text and its melodies. A section with nothing
/// recorded yet is still listed, with no melodies, so the book shows its whole shape.
/// </summary>
public sealed record BethGazoBookSectionDto(
    Guid Id,
    string Name,
    int Position,
    string? SyriacName,
    IReadOnlyList<BethGazoDescriptionDto> Descriptions,
    IReadOnlyList<Guid> AllowedModeIds,
    IReadOnlyList<BethGazoBookMelodyDto> Melodies);
