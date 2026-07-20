using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Lexicon.Application.Entries;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Owner-only editorial surface for the Lexicon (the backoffice write path).
/// Gated by the <c>api:v1:admin</c> scope. Unlike client apps, this is part of
/// Sabro itself — it may create, edit, delete, and change the lifecycle of entries.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/lexicon")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminLexiconController : ApiControllerBase
{
    // A single short recording per word, not a podcast — 5 MB comfortably covers minutes of audio.
    private const long MaxPronunciationAudioBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> PronunciationAudioExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["audio/mpeg"] = ".mp3",
        ["audio/mp3"] = ".mp3",
        ["audio/wav"] = ".wav",
        ["audio/x-wav"] = ".wav",
        ["audio/ogg"] = ".ogg",
        ["audio/webm"] = ".webm",
        ["audio/mp4"] = ".m4a",
        ["audio/x-m4a"] = ".m4a",
    };

    private readonly ILexiconEntryService entryService;

    public AdminLexiconController(ILexiconEntryService entryService)
    {
        this.entryService = entryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LexiconEntryDto>> Create(CreateLexiconEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await entryService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LexiconEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<LexiconEntryDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await entryService.ListAsync(page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexiconEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await entryService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexiconEntryDto>> Update(Guid id, UpdateLexiconEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await entryService.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var error = await entryService.DeleteAsync(id, cancellationToken);
        if (error is not null)
        {
            return FromError(error);
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexiconEntryDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await entryService.PublishAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexiconEntryDto>> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var result = await entryService.UnpublishAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/playable")]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LexiconEntryDto>> SetPlayable(Guid id, SetPlayableLexiconEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await entryService.SetPlayableAsync(id, request.Playable, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/pronunciation")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [RequestSizeLimit(MaxPronunciationAudioBytes)]
    public async Task<ActionResult<LexiconEntryDto>> UploadPronunciation(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return FromError(Error.Validation("An audio file is required."));
        }

        if (file.Length > MaxPronunciationAudioBytes)
        {
            return FromError(Error.Validation("The recording must be 5 MB or smaller."));
        }

        if (!PronunciationAudioExtensionsByContentType.TryGetValue(file.ContentType, out var extension))
        {
            return FromError(Error.Validation(
                $"Unsupported audio type '{file.ContentType}'. Use MP3, WAV, OGG, WebM, or M4A."));
        }

        await using var stream = file.OpenReadStream();
        var result = await entryService.UploadPronunciationAudioAsync(id, stream, extension, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/pronunciation")]
    [ProducesResponseType(typeof(LexiconEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LexiconEntryDto>> RemovePronunciation(Guid id, CancellationToken cancellationToken)
    {
        var result = await entryService.RemovePronunciationAudioAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
