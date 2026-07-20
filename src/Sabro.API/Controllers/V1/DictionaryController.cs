using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.Lexicon.Application.Dictionary;
using Sabro.Lexicon.Application.Search;
using Sabro.Lexicon.Domain;
using Sabro.Play.Application.Meltho;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The public dictionary — every published Lexicon entry, browsable and
/// searchable by anyone. Anonymous like the Meltho library (public,
/// non-personal content; still rate-limited): the hub's dictionary pages fetch
/// without an account. The payloads never carry editorial state or the
/// playable flag, so the future puzzle pool cannot be enumerated from here.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/dictionary")]
public sealed class DictionaryController : ApiControllerBase
{
    private readonly IDictionaryService dictionaryService;
    private readonly ILexiconSearchService searchService;
    private readonly IMelthoLibraryService melthoLibraryService;

    public DictionaryController(
        IDictionaryService dictionaryService,
        ILexiconSearchService searchService,
        IMelthoLibraryService melthoLibraryService)
    {
        this.dictionaryService = dictionaryService;
        this.searchService = searchService;
        this.melthoLibraryService = melthoLibraryService;
    }

    /// <summary>
    /// Browses published words alphabetically (ICU order on the unvocalized
    /// Syriac form), paged, optionally filtered to one grammatical category.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<DictionaryEntryListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DictionaryEntryListItem>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] GrammaticalCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var result = await dictionaryService.ListAsync(page, pageSize, category, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Typo-tolerant search over published words (Meilisearch: transliteration
    /// synonyms included). Same engine and shape as the authenticated
    /// lexicon-entries search — republished here for the anonymous dictionary.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<LexiconSearchHitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LexiconSearchHitDto>>> Search(
        [FromQuery] string? q = null,
        [FromQuery] GrammaticalCategory? category = null,
        [FromQuery] Guid? rootId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchAsync(q, category, rootId, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// One published word in full: info fields, root, per-letter composition,
    /// and whether it has appeared in Meltho on a past day (today's puzzle
    /// reports false until tomorrow — see <see cref="DictionaryEntryDetailResponse"/>).
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DictionaryEntryDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DictionaryEntryDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dictionaryService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        var detail = result.Value!;
        var playedInMeltho = await WasPlayedBeforeTodayAsync(id, cancellationToken);

        return Ok(new DictionaryEntryDetailResponse(
            detail.Id,
            detail.SyriacUnvocalized,
            detail.SyriacVocalized,
            detail.SblTransliteration,
            detail.TransliterationVariants,
            detail.GrammaticalCategory,
            detail.Morphology,
            detail.PlayableLength,
            detail.Root,
            detail.Meanings,
            detail.Composition,
            playedInMeltho,
            detail.PronunciationAudioUrl));
    }

    private async Task<bool> WasPlayedBeforeTodayAsync(Guid lexiconEntryId, CancellationToken cancellationToken)
    {
        var library = await melthoLibraryService.GetDetailAsync(lexiconEntryId, cancellationToken);
        if (!library.IsSuccess)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return library.Value!.PlayedOn.Any(date => date < today);
    }
}
