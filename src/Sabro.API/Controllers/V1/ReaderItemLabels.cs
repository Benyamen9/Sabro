using Sabro.BethGazo.Application.Chants;
using Sabro.Historical.Application.Figures;
using Sabro.Lexicon.Application.Entries;
using Sabro.Library.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Names the items of a reader's places and marks, composing the content modules' own read services at
/// the API layer (as <see cref="LibraryController"/> does), so the Library module depends on none of them.
/// Only published items get a label; the caller leaves the others out.
/// </summary>
internal sealed class ReaderItemLabels
{
    private readonly ILexiconLibraryReader lexiconReader;
    private readonly IHistoricalFigureService figureService;
    private readonly IPublicChantService chantService;

    public ReaderItemLabels(ILexiconLibraryReader lexiconReader, IHistoricalFigureService figureService, IPublicChantService chantService)
    {
        this.lexiconReader = lexiconReader;
        this.figureService = figureService;
        this.chantService = chantService;
    }

    public static string Key(string book, string itemKey) => $"{book}/{itemKey}";

    /// <summary>
    /// Labels every (book, item) given, keyed by <c>book/itemKey</c>. A grammar page maps to <c>null</c>
    /// (the hub names it); an item that is missing or unpublished is absent from the result.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, ReaderItemLabelDto?>> ResolveAsync(
        IEnumerable<(string Book, string ItemKey)> items,
        string language,
        CancellationToken cancellationToken)
    {
        var labels = new Dictionary<string, ReaderItemLabelDto?>(StringComparer.Ordinal);
        var byBook = items
            .Distinct()
            .GroupBy(i => i.Book, i => i.ItemKey)
            .ToDictionary(g => g.Key, g => g.ToList());

        if (byBook.TryGetValue(LibraryBooks.Grammar, out var pages))
        {
            foreach (var page in pages)
            {
                labels[Key(LibraryBooks.Grammar, page)] = null;
            }
        }

        if (byBook.TryGetValue(LibraryBooks.Lexicon, out var words))
        {
            await AddWordsAsync(labels, Ids(words), language, cancellationToken);
        }

        if (byBook.TryGetValue(LibraryBooks.BethSalme, out var figures))
        {
            await AddFiguresAsync(labels, Ids(figures), cancellationToken);
        }

        if (byBook.TryGetValue(LibraryBooks.BethGazo, out var chants))
        {
            await AddChantsAsync(labels, Ids(chants), cancellationToken);
        }

        return labels;
    }

    private static HashSet<Guid> Ids(IEnumerable<string> itemKeys) =>
        itemKeys.Select(k => Guid.TryParse(k, out var id) ? id : Guid.Empty).Where(id => id != Guid.Empty).ToHashSet();

    private static string Gloss(IReadOnlyList<LexiconMeaningDto> meanings, string language) =>
        (meanings.FirstOrDefault(m => m.Language == language)
            ?? meanings.FirstOrDefault(m => m.Language == "en")
            ?? (meanings.Count > 0 ? meanings[0] : null))?.Text ?? string.Empty;

    private async Task AddWordsAsync(Dictionary<string, ReaderItemLabelDto?> labels, HashSet<Guid> ids, string language, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        // The library reader serves unpublished words too (Meltho's archive keeps them), so filter first.
        var published = await lexiconReader.GetPublishedIdsAsync(ids, cancellationToken);
        var words = await lexiconReader.GetLibraryListAsync(published, cancellationToken);
        foreach (var word in words)
        {
            labels[Key(LibraryBooks.Lexicon, word.Id.ToString("D"))] = new ReaderItemLabelDto(
                Gloss(word.Meanings, language),
                word.SyriacVocalized ?? word.SyriacUnvocalized,
                word.SblTransliteration);
        }
    }

    private async Task AddFiguresAsync(Dictionary<string, ReaderItemLabelDto?> labels, HashSet<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        // The published roster is a few hundred figures: two pages at most, cheaper than one query per mark.
        for (var page = 1; ; page++)
        {
            var result = await figureService.ListPublishedAsync(null, null, null, null, page, PageRequest.MaxPageSize, cancellationToken);
            if (!result.IsSuccess)
            {
                return;
            }

            foreach (var figure in result.Value!.Items.Where(f => ids.Contains(f.Id)))
            {
                labels[Key(LibraryBooks.BethSalme, figure.Id.ToString("D"))] = new ReaderItemLabelDto(figure.Name, figure.SyriacName, null);
            }

            if (page * PageRequest.MaxPageSize >= result.Value.Total)
            {
                return;
            }
        }
    }

    private async Task AddChantsAsync(Dictionary<string, ReaderItemLabelDto?> labels, HashSet<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        // The book holds every published, recorded chant: the same set the hub's Beth Gazo page lists.
        var book = await chantService.GetBookAsync(cancellationToken);
        var modes = book.Modes.ToDictionary(m => m.Id, m => m.Name);
        foreach (var melody in book.Sections.SelectMany(s => s.Melodies))
        {
            foreach (var chant in melody.Chants.Where(c => ids.Contains(c.Id)))
            {
                var mode = chant.ModeId is { } modeId && modes.TryGetValue(modeId, out var name) ? name : null;
                labels[Key(LibraryBooks.BethGazo, chant.Id.ToString("D"))] = new ReaderItemLabelDto(
                    melody.Name,
                    chant.SyriacIncipitVocalized ?? chant.SyriacIncipit,
                    mode);
            }
        }
    }
}
