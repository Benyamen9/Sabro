namespace Sabro.Historical.Public;

/// <summary>
/// Read-only cross-module lookup of figure descriptions, consumed by the Play
/// module to show a player the stories of the figures they have found. The
/// description is deliberately absent from the public roster: finding a figure is
/// what reveals it, and only Play knows who found what.
/// </summary>
public interface IHistoricalFigureDescriptions
{
    /// <summary>
    /// Returns the descriptions of each requested figure that has any, keyed by
    /// figure id. A figure with none, or an unknown id, is simply absent. Not
    /// filtered by status: a figure once found stays readable even if unpublished.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<FigureDescription>>> GetDescriptionsAsync(
        IReadOnlyCollection<Guid> figureIds,
        CancellationToken cancellationToken);
}
