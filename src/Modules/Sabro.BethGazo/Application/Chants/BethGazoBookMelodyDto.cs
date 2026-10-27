namespace Sabro.BethGazo.Application.Chants;

/// <summary>One melody of a section and its recorded chants, in mode order.</summary>
public sealed record BethGazoBookMelodyDto(string Name, IReadOnlyList<BethGazoBookChantDto> Chants);
