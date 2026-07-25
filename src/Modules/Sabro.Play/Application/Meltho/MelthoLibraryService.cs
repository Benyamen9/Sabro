using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Domain;
using Sabro.Play.Infrastructure;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.Play.Application.Meltho;

internal sealed class MelthoLibraryService : IMelthoLibraryService
{
    private readonly PlayDbContext dbContext;
    private readonly ILexiconLibraryReader libraryReader;
    private readonly TimeProvider timeProvider;

    public MelthoLibraryService(
        PlayDbContext dbContext,
        ILexiconLibraryReader libraryReader,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.libraryReader = libraryReader;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<PagedResult<MelthoLibraryEntryDto>>> ListAsync(int page, int pageSize, LibrarySort sort, SortDirection? direction, string? search, CancellationToken cancellationToken)
    {
        var validationError = PageRequest.Validate(page, pageSize);
        if (validationError is not null)
        {
            return Result<PagedResult<MelthoLibraryEntryDto>>.Failure(validationError);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // One aggregate row per past word: when it was last served and how many days it appeared
        // on. The pool is the archive of past daily words (dozens, not millions), so the whole
        // set is materialised and sorted/paged in memory — alphabetical and length sort on
        // Lexicon fields, which live behind the projection rather than in this DbContext.
        var wordRows = await AggregateAsync(today, ids: null, cancellationToken);

        var ids = wordRows.Select(r => r.LexiconEntryId).ToList();
        var items = await libraryReader.GetLibraryListAsync(ids, cancellationToken);
        var byId = items.ToDictionary(i => i.Id);

        // A word whose entry was hard-deleted has no projection and drops out of the library.
        var merged = wordRows
            .Where(r => byId.ContainsKey(r.LexiconEntryId))
            .Select(r =>
            {
                var item = byId[r.LexiconEntryId];
                return new MelthoLibraryEntryDto(
                    r.LastPlayedOn,
                    item.Id,
                    item.SyriacUnvocalized,
                    item.SyriacVocalized,
                    item.SblTransliteration,
                    item.PlayableLength,
                    r.TimesPlayed,
                    item.Meanings.Select(m => new MelthoPuzzleMeaningDto(m.Language, m.Text)).ToArray());
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = Fold(search);
            merged = merged.Where(w => MatchesSearch(w, needle));
        }

        var ordered = Order(merged, sort, direction).ToList();
        var pageItems = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result<PagedResult<MelthoLibraryEntryDto>>.Success(
            new PagedResult<MelthoLibraryEntryDto>(pageItems, ordered.Count, page, pageSize));
    }

    public async Task<Result<MelthoLibraryDetailDto>> GetDetailAsync(Guid lexiconEntryId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Detail includes today (<=), unlike the browse list (< today). The id is only obtainable
        // from today's puzzle payload, so reaching today's detail means you've already played —
        // no reason to make you wait until tomorrow to look the word up. Future dates can't occur
        // (puzzles are created get-or-create for today only), but <= keeps the bound explicit.
        var playedOn = await dbContext.MelthoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Meltho && p.LexiconEntryId == lexiconEntryId && p.Date <= today)
            .Select(p => p.Date)
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken);
        if (playedOn.Count == 0)
        {
            return Result<MelthoLibraryDetailDto>.Failure(
                Error.NotFound("This word is not in the Meltho library yet."));
        }

        var detail = await libraryReader.GetLibraryDetailAsync(lexiconEntryId, cancellationToken);
        if (detail is null)
        {
            return Result<MelthoLibraryDetailDto>.Failure(
                Error.NotFound("This word could not be resolved."));
        }

        var dto = new MelthoLibraryDetailDto(
            detail.Id,
            detail.SyriacUnvocalized,
            detail.SyriacVocalized,
            detail.SblTransliteration,
            detail.TransliterationVariants,
            detail.GrammaticalCategory,
            detail.Morphology,
            detail.PlayableLength,
            detail.Root,
            detail.Meanings.Select(m => new MelthoPuzzleMeaningDto(m.Language, m.Text)).ToArray(),
            detail.Composition,
            playedOn,
            detail.PronunciationAudioUrl);

        return Result<MelthoLibraryDetailDto>.Success(dto);
    }

    public async Task<IReadOnlyDictionary<Guid, (DateOnly LastPlayedOn, int TimesPlayed)>> GetStatsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, (DateOnly, int)>();
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var rows = await AggregateAsync(today, ids, cancellationToken);
        return rows.ToDictionary(r => r.LexiconEntryId, r => (r.LastPlayedOn, r.TimesPlayed));
    }

    public async Task<Result<PagedResult<PlayedLibraryEntryDto>>> ListPlayedAndPublishedAsync(
        int page, int pageSize, LibrarySort sort, SortDirection? direction, string? search, CancellationToken cancellationToken)
    {
        var validationError = PageRequest.Validate(page, pageSize);
        if (validationError is not null)
        {
            return Result<PagedResult<PlayedLibraryEntryDto>>.Failure(validationError);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var wordRows = await AggregateAsync(today, ids: null, cancellationToken);

        // Unlike ListAsync, a word that has since been unpublished is dropped here — this is the
        // one behavioural difference between the standalone Meltho archive and the unified
        // library's "played" filter (see ListPlayedAndPublishedAsync's doc comment).
        var playedIds = wordRows.Select(r => r.LexiconEntryId).ToList();
        var publishedIds = (await libraryReader.GetPublishedIdsAsync(playedIds, cancellationToken)).ToHashSet();
        var stillPublished = wordRows.Where(r => publishedIds.Contains(r.LexiconEntryId)).ToList();

        var items = await libraryReader.GetLibraryListAsync(
            stillPublished.Select(r => r.LexiconEntryId).ToList(), cancellationToken);
        var byId = items.ToDictionary(i => i.Id);

        var merged = stillPublished
            .Where(r => byId.ContainsKey(r.LexiconEntryId))
            .Select(r =>
            {
                var item = byId[r.LexiconEntryId];
                return new PlayedLibraryEntryDto(
                    item.Id,
                    item.SyriacUnvocalized,
                    item.SyriacVocalized,
                    item.SblTransliteration,
                    item.GrammaticalCategory,
                    item.PlayableLength,
                    item.Meanings.Select(m => new MelthoPuzzleMeaningDto(m.Language, m.Text)).ToArray(),
                    r.LastPlayedOn,
                    r.TimesPlayed);
            });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = Fold(search);
            merged = merged.Where(w => MatchesSearch(w, needle));
        }

        var ordered = Order(merged, sort, direction).ToList();
        var pageItems = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result<PagedResult<PlayedLibraryEntryDto>>.Success(
            new PagedResult<PlayedLibraryEntryDto>(pageItems, ordered.Count, page, pageSize));
    }

    private static IEnumerable<T> Order<T>(IEnumerable<T> words, LibrarySort sort, SortDirection? direction)
        where T : ISortableLibraryWord
    {
        var descending = (direction ?? NaturalDirection(sort)) == SortDirection.Descending;

        // The secondary key (Syriac, ordinal — i.e. abjad order) only breaks ties and stays
        // ascending so equal-rank rows read in a stable, predictable order either way.
        return sort switch
        {
            LibrarySort.Alphabetical => descending
                ? words.OrderByDescending(w => w.SyriacUnvocalized, StringComparer.Ordinal)
                : words.OrderBy(w => w.SyriacUnvocalized, StringComparer.Ordinal),
            LibrarySort.Length => descending
                ? words.OrderByDescending(w => w.PlayableLength).ThenBy(w => w.SyriacUnvocalized, StringComparer.Ordinal)
                : words.OrderBy(w => w.PlayableLength).ThenBy(w => w.SyriacUnvocalized, StringComparer.Ordinal),
            _ => descending
                ? words.OrderByDescending(w => w.LastPlayedOn).ThenBy(w => w.SyriacUnvocalized, StringComparer.Ordinal)
                : words.OrderBy(w => w.LastPlayedOn).ThenBy(w => w.SyriacUnvocalized, StringComparer.Ordinal),
        };
    }

    // Recent reads newest-first; the textual/size sorts read smallest-first.
    private static SortDirection NaturalDirection(LibrarySort sort) =>
        sort == LibrarySort.Recent ? SortDirection.Descending : SortDirection.Ascending;

    private static bool MatchesSearch<T>(T word, string needle)
        where T : ISortableLibraryWord
    {
        if (Fold(word.SyriacUnvocalized).Contains(needle, StringComparison.Ordinal))
        {
            return true;
        }

        if (word.SblTransliteration is { } transliteration && Fold(transliteration).Contains(needle, StringComparison.Ordinal))
        {
            return true;
        }

        return word.Meanings.Any(m => Fold(m.Text).Contains(needle, StringComparison.Ordinal));
    }

    // Lower-cases and strips combining marks so a query without diacritics (e.g. "ktobo") still
    // matches an SBL transliteration that carries them (e.g. "ktōbō"). Syriac base letters are not
    // combining marks, so unvocalized forms pass through unchanged.
    private static string Fold(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().ToLowerInvariant();
    }

    // Shared by ListAsync (ids: null, whole table), GetStatsAsync (ids: a specific page, narrows
    // the query itself), and ListPlayedAndPublishedAsync (ids: null, then filtered by publish
    // state afterward — it needs the full played set before it knows which ids survive).
    private async Task<List<AggregateRow>> AggregateAsync(DateOnly today, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken)
    {
        var query = dbContext.MelthoDailyPuzzles
            .AsNoTracking()
            .Where(p => p.GameId == Games.Meltho && p.Date < today);
        if (ids is not null)
        {
            var idList = ids.ToList();
            query = query.Where(p => idList.Contains(p.LexiconEntryId));
        }

        return await query
            .GroupBy(p => p.LexiconEntryId)
            .Select(g => new AggregateRow(g.Key, g.Max(p => p.Date), g.Count()))
            .ToListAsync(cancellationToken);
    }

    private sealed record AggregateRow(Guid LexiconEntryId, DateOnly LastPlayedOn, int TimesPlayed);
}
