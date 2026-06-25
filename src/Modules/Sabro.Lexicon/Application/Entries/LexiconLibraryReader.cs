using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Text;

namespace Sabro.Lexicon.Application.Entries;

internal sealed class LexiconLibraryReader : ILexiconLibraryReader
{
    private readonly LexiconDbContext dbContext;

    public LexiconLibraryReader(LexiconDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<LexiconLibraryListItem>> GetLibraryListAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return Array.Empty<LexiconLibraryListItem>();
        }

        var idList = ids.ToList();
        var entries = await dbContext.Entries
            .AsNoTracking()
            .Where(e => idList.Contains(e.Id))
            .ToListAsync(cancellationToken);

        return entries
            .Select(e => new LexiconLibraryListItem(
                e.Id,
                e.SyriacUnvocalized,
                e.SblTransliteration,
                e.PlayableLength,
                e.Meanings.Select(m => new LexiconMeaningDto(m.Language, m.Text)).ToArray()))
            .ToList();
    }

    public async Task<LexiconLibraryDetail?> GetLibraryDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        // Optional Semitic root, surfaced as its Syriac form for the detail page's root chip.
        var root = entry.RootId is { } rootId
            ? await dbContext.Roots
                .AsNoTracking()
                .Where(r => r.Id == rootId)
                .Select(r => r.Form)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new LexiconLibraryDetail(
            entry.Id,
            entry.SyriacUnvocalized,
            entry.SyriacVocalized,
            entry.SblTransliteration,
            entry.TransliterationVariants.ToArray(),
            entry.GrammaticalCategory.ToString(),
            entry.Morphology,
            entry.PlayableLength,
            root,
            entry.Meanings.Select(m => new LexiconMeaningDto(m.Language, m.Text)).ToArray(),
            SyriacComposition.Decompose(entry.SyriacVocalized));
    }
}
