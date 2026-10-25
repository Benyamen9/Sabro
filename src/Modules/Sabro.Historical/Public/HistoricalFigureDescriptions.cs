using Microsoft.EntityFrameworkCore;
using Sabro.Historical.Infrastructure;

namespace Sabro.Historical.Public;

internal sealed class HistoricalFigureDescriptions : IHistoricalFigureDescriptions
{
    private readonly HistoricalDbContext dbContext;

    public HistoricalFigureDescriptions(HistoricalDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<FigureDescription>>> GetDescriptionsAsync(
        IReadOnlyCollection<Guid> figureIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(figureIds);
        if (figureIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<FigureDescription>>();
        }

        // Descriptions are owned by the figure and load with it; there is no
        // navigation to Include (see HistoricalFigureProposalTargetSource).
        var ids = figureIds.ToArray();
        var figures = await dbContext.Figures
            .AsNoTracking()
            .Where(figure => ids.Contains(figure.Id))
            .ToListAsync(cancellationToken);

        return figures
            .Where(figure => figure.Descriptions.Count > 0)
            .ToDictionary(
                figure => figure.Id,
                figure => (IReadOnlyList<FigureDescription>)figure.Descriptions
                    .OrderBy(d => d.Language, StringComparer.Ordinal)
                    .Select(d => new FigureDescription(d.Language, d.Text))
                    .ToArray());
    }
}
