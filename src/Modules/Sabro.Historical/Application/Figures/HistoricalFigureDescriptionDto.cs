namespace Sabro.Historical.Application.Figures;

/// <summary>
/// One figure description in one language, as carried over the API. Part of the
/// <c>/api/v1/</c> contract.
/// </summary>
public sealed record HistoricalFigureDescriptionDto(string Language, string Text);
