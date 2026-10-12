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

        // Every mode, not only the ones a published chant currently uses: trimming
        // this would narrow the answer space for free.
        var modes = await dbContext.Modes
            .AsNoTracking()
            .OrderBy(m => m.Position)
            .Select(m => new BethGazoModeDto(m.Id, m.Name, m.Position))
            .ToListAsync(cancellationToken);

        // Likewise every section, used or not. Each carries the modes it admits so
        // the round knows whether to ask for one — see BethGazoSectionDto for why
        // that is not the join the melodies are protected from.
        var sections = await dbContext.Sections
            .AsNoTracking()
            .OrderBy(s => s.Position)
            .Select(s => new BethGazoSectionDto(
                s.Id,
                s.Name,
                s.Position,
                s.AllowedModes.Select(m => m.ModeId).ToList()))
            .ToListAsync(cancellationToken);

        // A mode every moded section admits is always offered; one that only some
        // admit (the mshaḥelfotho, farde-only) is conditional. Derived, not named.
        var modedSections = sections.Where(s => s.AllowedModeIds.Count > 0).ToList();
        var alwaysOffered = modedSections.Count == 0
            ? modes.Select(m => m.Id).ToHashSet()
            : modedSections
                .Skip(1)
                .Aggregate(
                    modedSections[0].AllowedModeIds.ToHashSet(),
                    (common, section) =>
                    {
                        common.IntersectWith(section.AllowedModeIds);
                        return common;
                    });
        var conditionalModeIds = modes
            .Where(m => !alwaysOffered.Contains(m.Id))
            .Select(m => m.Id)
            .ToList();
        var conditional = conditionalModeIds.ToHashSet();

        // The one melody-to-mode projection this endpoint makes, and it is reduced
        // before it leaves: only "has any mode" and the conditional modes survive,
        // never the always-offered mode a chant is sung in.
        var melodyModes = await dbContext.Chants
            .AsNoTracking()
            .Where(c => c.Status == ChantStatus.Published)
            .Select(c => new { c.Transliteration, c.ModeId })
            .Distinct()
            .ToListAsync(cancellationToken);
        var hints = melodyModes
            .GroupBy(c => c.Transliteration)
            .Select(group => new MelodyModeHintDto(
                group.Key,
                group.Any(c => c.ModeId != null),
                group
                    .Where(c => c.ModeId is Guid id && conditional.Contains(id))
                    .Select(c => c.ModeId!.Value)
                    .Distinct()
                    .ToList()))
            .Where(hint => !hint.HasModes || hint.ConditionalModeIds.Count > 0)
            .OrderBy(hint => hint.Melody, StringComparer.Ordinal)
            .ToList();

        return new ChantAnswerOptionsDto(melodies, sections, modes, conditionalModeIds, hints);
    }
}
