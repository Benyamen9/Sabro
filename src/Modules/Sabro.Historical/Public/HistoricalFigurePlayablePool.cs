using Microsoft.EntityFrameworkCore;
using Sabro.Historical.Domain;
using Sabro.Historical.Infrastructure;

namespace Sabro.Historical.Public;

internal sealed class HistoricalFigurePlayablePool : IHistoricalFigurePlayablePool
{
    private readonly HistoricalDbContext dbContext;

    public HistoricalFigurePlayablePool(HistoricalDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> GetEligibleFigureIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Figures
            .AsNoTracking()
            .Where(e => e.Status == HistoricalFigureStatus.Published && e.PlayableInShmo)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlayableHistoricalFigure?> GetPlayableFigureAsync(Guid id, CancellationToken cancellationToken)
    {
        var figure = await dbContext.Figures
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (figure is null)
        {
            return null;
        }

        return new PlayableHistoricalFigure(
            figure.Id,
            figure.Name,
            figure.Category,
            figure.Era,
            figure.Period,
            figure.Role,
            figure.Region,
            figure.Tradition,
            figure.Gender);
    }
}
