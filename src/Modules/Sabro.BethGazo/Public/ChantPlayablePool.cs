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
        // Joined to the mode table rather than carrying the id outward: Play
        // should not have to know this module has a reference table at all.
        var projection = await dbContext.Chants
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Join(
                dbContext.Modes.AsNoTracking(),
                chant => chant.ModeId,
                mode => mode.Id,
                (chant, mode) => new { chant, mode.Name })
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
            projection.Name,
            chant.Shuhlofo,
            chant.AudioUrl);
    }
}
