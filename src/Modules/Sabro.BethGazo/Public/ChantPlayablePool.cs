using Microsoft.EntityFrameworkCore;
using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Infrastructure;

namespace Sabro.BethGazo.Public;

internal sealed class ChantPlayablePool : IChantPlayablePool
{
    private readonly BethGazoDbContext dbContext;

    public ChantPlayablePool(BethGazoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> GetEligibleChantIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Chants
            .AsNoTracking()
            .Where(e => e.Status == ChantStatus.Published && e.PlayableInNahlo)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlayableChant?> GetPlayableChantAsync(Guid id, CancellationToken cancellationToken)
    {
        // Joined to the reference tables rather than carrying ids outward: Play
        // should not have to know this module has reference tables at all.
        //
        // The mode join is a LEFT join (GroupJoin + DefaultIfEmpty). An inner join
        // was right while every chant had a mode; now that the madroshe have none,
        // it would return null here for every one of them — and null is this
        // method's "cannot render", so the daily puzzle would 409 on any madrosho
        // as though the pool were empty.
        var projection = await (
            from row in dbContext.Chants.AsNoTracking().Where(e => e.Id == id)
            join section in dbContext.Sections.AsNoTracking() on row.SectionId equals section.Id
            join mode in dbContext.Modes.AsNoTracking() on row.ModeId equals mode.Id into modes
            from mode in modes.DefaultIfEmpty()
            select new { chant = row, SectionName = section.Name, ModeName = mode != null ? mode.Name : null })
            .FirstOrDefaultAsync(cancellationToken);

        if (projection is null)
        {
            return null;
        }

        var chant = projection.chant;

        // A published chant always has audio (Chant.Publish enforces it), but a
        // draft served through a stale daily-puzzle row might not. Treat a
        // missing recording as "cannot render" rather than shipping a null URL
        // the client would silently fail to play.
        if (string.IsNullOrWhiteSpace(chant.AudioUrl))
        {
            return null;
        }

        return new PlayableChant(
            chant.Id,
            chant.SyriacIncipit,
            chant.SyriacIncipitVocalized,
            chant.Transliteration,
            projection.SectionName,
            projection.ModeName,
            chant.VariantKind,
            chant.VariantNumber,
            chant.AudioUrl);
    }
}
