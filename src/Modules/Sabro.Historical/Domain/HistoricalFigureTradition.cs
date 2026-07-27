namespace Sabro.Historical.Domain;

/// <summary>
/// A figure's ecclesiastical tradition. <see cref="NotApplicable"/> is itself an
/// informative exact-match value for the Shmo guessing hint (e.g. pre-Christian
/// Biblical figures) — not a null-shaped special case. The field is still nullable
/// at the entity level to represent "not yet decided" while a figure is a draft.
/// </summary>
public enum HistoricalFigureTradition
{
    WestSyriac,
    EastSyriac,
    ByzantineChalcedonian,
    NotApplicable,
}
