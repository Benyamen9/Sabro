namespace Sabro.Historical.Application.Figures;

/// <summary>
/// A description supplied on create or update. Shared by both because the shape
/// is identical: descriptions are always sent as the complete set for the figure,
/// never patched one language at a time.
/// </summary>
public sealed record HistoricalFigureDescriptionRequest(string Language, string Text);
