using System.Globalization;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.Lexicon.Application.Dictionary;
using Sabro.Lexicon.Application.Entries;
using Sabro.Play.Application.Meltho;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The unified word library behind Sabro's own `/library` page: every published word, optionally
/// filtered to just the ones that have appeared in Meltho. Additive alongside the existing
/// `/api/v1/dictionary` and `/play/meltho/library` endpoints — neither of those changes; this one
/// composes both modules' existing read services at the API layer, the same way
/// <see cref="DictionaryController"/> already does for <c>playedInMeltho</c> on the detail route.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/library")]
public sealed class LibraryController : ApiControllerBase
{
    private readonly IDictionaryService dictionaryService;
    private readonly IMelthoLibraryService melthoLibraryService;

    public LibraryController(IDictionaryService dictionaryService, IMelthoLibraryService melthoLibraryService)
    {
        this.dictionaryService = dictionaryService;
        this.melthoLibraryService = melthoLibraryService;
    }

    /// <summary>
    /// Lists words for the unified library. <c>playedInMeltho=false</c> (default) browses every
    /// published word, Alphabetical/Length sort only — "Recent" is rejected, since most words have
    /// no play history to sort by. <c>playedInMeltho=true</c> lists only the published words that
    /// have been served as a Meltho daily puzzle at least once (a word that has since been
    /// unpublished drops out here, even though it stays reachable via the standalone Meltho
    /// archive) — all three sorts apply there, including Recent.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<UnifiedLibraryEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UnifiedLibraryEntryDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] LibrarySort sort = LibrarySort.Alphabetical,
        [FromQuery] SortDirection? direction = null,
        [FromQuery] bool playedInMeltho = false,
        CancellationToken cancellationToken = default)
    {
        var pageError = PageRequest.Validate(page, pageSize);
        if (pageError is not null)
        {
            return FromError(pageError);
        }

        if (playedInMeltho)
        {
            var playedResult = await melthoLibraryService.ListPlayedAndPublishedAsync(page, pageSize, sort, direction, search, cancellationToken);
            if (!playedResult.IsSuccess)
            {
                return FromError(playedResult.Error!);
            }

            var playedItems = playedResult.Value!.Items.Select(ToDto).ToArray();
            return Ok(new PagedResult<UnifiedLibraryEntryDto>(playedItems, playedResult.Value.Total, page, pageSize));
        }

        if (sort == LibrarySort.Recent)
        {
            return FromError(Error.Validation("Recent sort is only available when playedInMeltho=true."));
        }

        // The full published set is a few dozen words at current scale — same "dozens, not
        // millions" justification the Meltho archive already relies on — so it's fetched whole and
        // sorted/searched/paged in memory rather than pushing Length sort or search into SQL.
        var dictionaryResult = await dictionaryService.ListAsync(1, PageRequest.MaxPageSize, category: null, cancellationToken);
        if (!dictionaryResult.IsSuccess)
        {
            return FromError(dictionaryResult.Error!);
        }

        var allWords = dictionaryResult.Value!.Items;
        var ids = allWords.Select(w => w.Id).ToList();
        var stats = await melthoLibraryService.GetStatsAsync(ids, cancellationToken);

        IEnumerable<UnifiedLibraryEntryDto> merged = allWords.Select(w => ToDto(w, stats));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = Fold(search);
            merged = merged.Where(w => MatchesSearch(w, needle));
        }

        var ordered = Order(merged, sort, direction).ToList();
        var pageItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new PagedResult<UnifiedLibraryEntryDto>(pageItems, ordered.Count, page, pageSize));
    }

    private static UnifiedLibraryEntryDto ToDto(DictionaryEntryListItem word, IReadOnlyDictionary<Guid, (DateOnly LastPlayedOn, int TimesPlayed)> stats)
    {
        var hasStats = stats.TryGetValue(word.Id, out var stat);
        return new UnifiedLibraryEntryDto(
            word.Id,
            word.SyriacUnvocalized,
            word.SyriacVocalized,
            word.SblTransliteration,
            word.GrammaticalCategory,
            word.LetterCount,
            word.Meanings,
            hasStats ? stat.LastPlayedOn : null,
            hasStats ? stat.TimesPlayed : null);
    }

    private static UnifiedLibraryEntryDto ToDto(PlayedLibraryEntryDto word) => new(
        word.Id,
        word.SyriacUnvocalized,
        word.SyriacVocalized,
        word.SblTransliteration,
        word.GrammaticalCategory,
        word.PlayableLength,
        word.Meanings.Select(m => new Sabro.Lexicon.Application.Entries.LexiconMeaningDto(m.Language, m.Text)).ToArray(),
        word.LastPlayedOn,
        word.TimesPlayed);

    // The secondary key (Syriac, ordinal — abjad order) only breaks ties and stays ascending so
    // equal-rank rows read in a stable, predictable order either way. Mirrors
    // MelthoLibraryService.Order's shape for the two sorts valid in this (off) state.
    private static IEnumerable<UnifiedLibraryEntryDto> Order(IEnumerable<UnifiedLibraryEntryDto> words, LibrarySort sort, SortDirection? direction)
    {
        var descending = direction == SortDirection.Descending;
        return sort == LibrarySort.Length
            ? descending
                ? words.OrderByDescending(w => w.LetterCount).ThenBy(w => w.SyriacUnvocalized, StringComparer.Ordinal)
                : words.OrderBy(w => w.LetterCount).ThenBy(w => w.SyriacUnvocalized, StringComparer.Ordinal)
            : descending
                ? words.OrderByDescending(w => w.SyriacUnvocalized, StringComparer.Ordinal)
                : words.OrderBy(w => w.SyriacUnvocalized, StringComparer.Ordinal);
    }

    private static bool MatchesSearch(UnifiedLibraryEntryDto word, string needle)
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
    // matches an SBL transliteration that carries them (e.g. "ktōbō") — same rule as
    // MelthoLibraryService.Fold, duplicated rather than shared across the module boundary.
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
}
