using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sabro.API.Configuration;
using Sabro.BethGazo.Application.Chants;
using Sabro.Historical.Application.Figures;
using Sabro.Lexicon.Application.Entries;
using Sabro.Library.Application.Reading;
using Sabro.Library.Domain;
using Sabro.Shared.Results;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The signed-in reader's own Library: the red ribbon in each book (placed automatically, last item
/// opened) and their ܀ marks. Signed out, the hub keeps both on the device and sends them up on the next
/// signed-in visit, passing the original times so the most recent place wins and marks keep their dates.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/library/me")]
[Authorize(Policy = AuthPolicies.Write)]
public sealed class ReaderController : ApiControllerBase
{
    private readonly IReaderService readerService;
    private readonly ReaderItemLabels labels;

    public ReaderController(
        IReaderService readerService,
        ILexiconLibraryReader lexiconReader,
        IHistoricalFigureService figureService,
        IPublicChantService chantService)
    {
        this.readerService = readerService;
        labels = new ReaderItemLabels(lexiconReader, figureService, chantService);
    }

    /// <summary>
    /// Every place and mark of the caller, each with a label in <paramref name="language"/> (a word's gloss
    /// falls back to English). Items no longer published are left out.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ReaderShelfDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReaderShelfDto>> Get([FromQuery] string? language, CancellationToken cancellationToken)
    {
        var userId = ResolveLogtoUserId();
        if (!userId.IsSuccess)
        {
            return FromError(userId.Error!);
        }

        var shelf = await readerService.GetShelfAsync(userId.Value!, cancellationToken);
        if (!shelf.IsSuccess)
        {
            return FromError(shelf.Error!);
        }

        var items = shelf.Value!.Places.Select(p => (p.Book, p.ItemKey))
            .Concat(shelf.Value.Marks.Select(m => (m.Book, m.ItemKey)));
        var resolved = await labels.ResolveAsync(items, NormalizeLanguage(language), cancellationToken);

        var places = new List<ReaderPlaceDto>();
        foreach (var place in shelf.Value.Places)
        {
            if (resolved.TryGetValue(ReaderItemLabels.Key(place.Book, place.ItemKey), out var label))
            {
                places.Add(new ReaderPlaceDto(place.Book, place.ItemKey, place.OpenedAt, label));
            }
        }

        var marks = new List<ReaderMarkDto>();
        foreach (var mark in shelf.Value.Marks)
        {
            if (resolved.TryGetValue(ReaderItemLabels.Key(mark.Book, mark.ItemKey), out var label))
            {
                marks.Add(new ReaderMarkDto(mark.Book, mark.ItemKey, mark.MarkedAt, label));
            }
        }

        return Ok(new ReaderShelfDto(places, marks));
    }

    /// <summary>
    /// Moves the ribbon in <paramref name="book"/> to the item just opened. A place opened before the one
    /// already kept (sent late from another device) leaves the ribbon where it is; the kept place is returned.
    /// </summary>
    [HttpPut("places/{book}")]
    [ProducesResponseType(typeof(ReaderPlaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReaderPlaceDto>> SetPlace(
        string book,
        SetReadingPlaceRequest request,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        var userId = ResolveLogtoUserId();
        if (!userId.IsSuccess)
        {
            return FromError(userId.Error!);
        }

        var item = await FindPublishedAsync(book, request.ItemKey, language, cancellationToken);
        if (!item.IsSuccess)
        {
            return FromError(item.Error!);
        }

        var result = await readerService.SetPlaceAsync(userId.Value!, book, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        var place = result.Value!;

        // The kept place may be another, newer one: name that one, not the item sent.
        var label = place.ItemKey == item.Value!.ItemKey
            ? item.Value.Label
            : (await labels.ResolveAsync([(place.Book, place.ItemKey)], NormalizeLanguage(language), cancellationToken))
                .GetValueOrDefault(ReaderItemLabels.Key(place.Book, place.ItemKey));
        return Ok(new ReaderPlaceDto(place.Book, place.ItemKey, place.OpenedAt, label));
    }

    /// <summary>
    /// Marks an item ܀. Answers 201 when this call made the mark and 200 when it was marked already, so a
    /// retry is harmless. The optional body keeps the date of a mark made on the device while signed out.
    /// </summary>
    [HttpPut("marks/{book}/{itemKey}")]
    [ProducesResponseType(typeof(ReaderMarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReaderMarkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReaderMarkDto>> Mark(
        string book,
        string itemKey,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MarkItemRequest? request,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        var userId = ResolveLogtoUserId();
        if (!userId.IsSuccess)
        {
            return FromError(userId.Error!);
        }

        var item = await FindPublishedAsync(book, itemKey, language, cancellationToken);
        if (!item.IsSuccess)
        {
            return FromError(item.Error!);
        }

        var result = await readerService.MarkAsync(userId.Value!, book, itemKey, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        var mark = result.Value!.Mark;
        var dto = new ReaderMarkDto(mark.Book, mark.ItemKey, mark.MarkedAt, item.Value!.Label);
        return result.Value.WasCreated ? StatusCode(StatusCodes.Status201Created, dto) : Ok(dto);
    }

    /// <summary>Removes a mark. Removing one that is not there also answers 204, so a retry is harmless.</summary>
    [HttpDelete("marks/{book}/{itemKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Unmark(string book, string itemKey, CancellationToken cancellationToken)
    {
        var userId = ResolveLogtoUserId();
        if (!userId.IsSuccess)
        {
            return FromError(userId.Error!);
        }

        var error = await readerService.UnmarkAsync(userId.Value!, book, itemKey, cancellationToken);
        return error is null ? NoContent() : FromError(error);
    }

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();

    /// <summary>
    /// Checks the item exists and is published before a place or a mark is kept for it, so the tables hold
    /// only things the Library can show. Grammar pages live in the hub and are taken on trust.
    /// </summary>
    private async Task<Result<(string ItemKey, ReaderItemLabelDto? Label)>> FindPublishedAsync(
        string book,
        string? itemKey,
        string? language,
        CancellationToken cancellationToken)
    {
        var normalizedBook = LibraryItemKey.NormalizeBook(book);
        if (!normalizedBook.IsSuccess)
        {
            return Result<(string, ReaderItemLabelDto?)>.Failure(normalizedBook.Error!);
        }

        var normalizedItem = LibraryItemKey.Normalize(normalizedBook.Value!, itemKey);
        if (!normalizedItem.IsSuccess)
        {
            return Result<(string, ReaderItemLabelDto?)>.Failure(normalizedItem.Error!);
        }

        var resolved = await labels.ResolveAsync(
            [(normalizedBook.Value!, normalizedItem.Value!)],
            NormalizeLanguage(language),
            cancellationToken);
        return resolved.TryGetValue(ReaderItemLabels.Key(normalizedBook.Value!, normalizedItem.Value!), out var label)
            ? Result<(string, ReaderItemLabelDto?)>.Success((normalizedItem.Value!, label))
            : Result<(string, ReaderItemLabelDto?)>.Failure(Error.NotFound("Published item"));
    }
}
