namespace Sabro.Historical.Domain;

/// <summary>
/// Editorial lifecycle of a <see cref="HistoricalFigure"/>. A <see cref="Draft"/> may
/// hold partial data; only a <see cref="Published"/> figure (every classification
/// field set) may be marked playable or served to clients.
/// </summary>
public enum HistoricalFigureStatus
{
    Draft,
    Published,
}
