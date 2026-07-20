using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Application.Entries;
using Sabro.Lexicon.Domain;
using Sabro.Lexicon.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;
using Sabro.Shared.Text;

namespace Sabro.Lexicon.Application.Dictionary;

internal sealed class DictionaryService : IDictionaryService
{
    private readonly LexiconDbContext dbContext;

    public DictionaryService(LexiconDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Result<PagedResult<DictionaryEntryListItem>>> ListAsync(
        int page,
        int pageSize,
        GrammaticalCategory? category,
        CancellationToken cancellationToken)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return Result<PagedResult<DictionaryEntryListItem>>.Failure(pageError);
        }

        var query = dbContext.Entries
            .AsNoTracking()
            .Where(e => e.Status == LexiconEntryStatus.Published);
        if (category is not null)
        {
            query = query.Where(e => e.GrammaticalCategory == category.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderBy(e => e.SyriacUnvocalized)
            .ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entries
            .Select(e => new DictionaryEntryListItem(
                e.Id,
                e.SyriacUnvocalized,
                e.SyriacVocalized,
                e.SblTransliteration,
                e.GrammaticalCategory.ToString(),
                e.PlayableLength,
                e.Meanings.Select(m => new LexiconMeaningDto(m.Language, m.Text)).ToArray()))
            .ToArray();

        return Result<PagedResult<DictionaryEntryListItem>>.Success(
            new PagedResult<DictionaryEntryListItem>(items, total, page, pageSize));
    }

    public async Task<Result<LexiconLibraryDetail>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Entries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.Status == LexiconEntryStatus.Published, cancellationToken);
        if (entry is null)
        {
            return Result<LexiconLibraryDetail>.Failure(
                Error.NotFound("This word is not in the dictionary."));
        }

        var root = entry.RootId is { } rootId
            ? await dbContext.Roots
                .AsNoTracking()
                .Where(r => r.Id == rootId)
                .Select(r => r.Form)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return Result<LexiconLibraryDetail>.Success(new LexiconLibraryDetail(
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
            SyriacComposition.Decompose(entry.SyriacVocalized),
            entry.PronunciationAudioUrl));
    }
}
