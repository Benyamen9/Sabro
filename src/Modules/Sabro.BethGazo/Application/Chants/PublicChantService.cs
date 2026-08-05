using Microsoft.EntityFrameworkCore;
using Sabro.BethGazo.Domain;
using Sabro.BethGazo.Infrastructure;

namespace Sabro.BethGazo.Application.Chants;

internal sealed class PublicChantService : IPublicChantService
{
    private readonly BethGazoDbContext dbContext;

    public PublicChantService(BethGazoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ChantAnswerOptionsDto> GetAnswerOptionsAsync(CancellationToken cancellationToken)
    {
        // Three separate queries, not one projection over the chants. Joining them
        // is what would leak the mode — see ChantAnswerOptionsDto.
        var melodies = await dbContext.Chants
            .AsNoTracking()
            .Where(c => c.Status == ChantStatus.Published)
            .Select(c => c.Transliteration)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);

        var shuhlofe = await dbContext.Chants
            .AsNoTracking()
            .Where(c => c.Status == ChantStatus.Published && c.Shuhlofo != null)
            .Select(c => c.Shuhlofo!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

        // Every mode, not only the ones a published chant currently uses: trimming
        // this would narrow the answer space for free.
        var modes = await dbContext.Modes
            .AsNoTracking()
            .OrderBy(m => m.Position)
            .Select(m => new BethGazoModeDto(m.Id, m.Name, m.Position))
            .ToListAsync(cancellationToken);

        return new ChantAnswerOptionsDto(melodies, modes, shuhlofe);
    }
}
