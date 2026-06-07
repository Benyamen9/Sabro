using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;

namespace Sabro.Lexicon.Application.Entries;

internal sealed class LexiconPlayablePool : ILexiconPlayablePool
{
    private readonly LexiconDbContext dbContext;

    public LexiconPlayablePool(LexiconDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Guid>> GetEligibleEntryIdsAsync(int minLength, int maxLength, CancellationToken cancellationToken)
    {
        return await dbContext.Entries
            .AsNoTracking()
            .Where(e => e.Status == LexiconEntryStatus.Published
                && e.PlayableInMeltha
                && e.PlayableLength >= minLength
                && e.PlayableLength <= maxLength)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlayableLexiconEntry?> GetPlayableEntryAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        return new PlayableLexiconEntry(
            entry.Id,
            entry.SyriacUnvocalized,
            entry.SyriacVocalized,
            entry.SblTransliteration,
            entry.PlayableLength,
            entry.Meanings.Select(m => new LexiconMeaningDto(m.Language, m.Text)).ToArray());
    }
}
