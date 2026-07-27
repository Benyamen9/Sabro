namespace Sabro.Historical.Domain;

/// <summary>A figure's single primary role. Multi-role scoring is deferred until a figure genuinely won't fit one bucket.</summary>
public enum HistoricalFigureRole
{
    Prophet,
    King,
    Judge,
    Apostle,
    Evangelist,
    Patriarch,
    Bishop,
    Translator,
    Commentator,
    Monk,
    Martyr,
    Other,
}
