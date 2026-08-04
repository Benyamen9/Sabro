using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.API.Media;
using Sabro.BethGazo.Application.Chants;
using Sabro.BethGazo.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Editorial surface for the Beth Gazo — the chants Nahlo draws its daily puzzle
/// from (the backoffice write path). Gated by the <c>api:v1:admin</c> scope plus a
/// Nahlo-area grant. Unlike client apps, this is part of Sabro itself — it may
/// create, edit, delete, and change the lifecycle of chants.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/chants")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminChantsController : ApiControllerBase
{
    /// <summary>
    /// Larger than the Lexicon's 5 MB pronunciation cap: a chant is a sung phrase
    /// rather than a single word, so the recordings are longer.
    /// </summary>
    private const long MaxChantAudioBytes = 15 * 1024 * 1024;

    private readonly IChantService chantService;

    public AdminChantsController(IChantService chantService)
    {
        this.chantService = chantService;
    }

    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpPost]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChantDto>> Create(CreateChantRequest request, CancellationToken cancellationToken)
    {
        var result = await chantService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    /// <summary>
    /// Lists chants for the backoffice — Draft and Published alike. A direct
    /// relational query: the treasury is small enough that a search index would be
    /// plumbing without payoff.
    /// </summary>
    [Authorize(Policy = AuthPolicies.ChantsView)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ChantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ChantDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] ChantStatus? status = null,
        [FromQuery] Guid? modeId = null,
        [FromQuery] bool? playableInNahlo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await chantService.ListAsync(
            search, status, modeId, playableInNahlo, page, pageSize, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    /// <summary>
    /// The modes, in traditional order. Served rather than hardcoded in the client:
    /// the set grows as the owner works through the tradition, and some sets run
    /// past eight — a client-side constant would silently omit them.
    /// </summary>
    [Authorize(Policy = AuthPolicies.ChantsView)]
    [HttpGet("modes")]
    [ProducesResponseType(typeof(IReadOnlyList<BethGazoModeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BethGazoModeDto>>> ListModes(CancellationToken cancellationToken) =>
        Ok(await chantService.ListModesAsync(cancellationToken));

    [Authorize(Policy = AuthPolicies.ChantsView)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChantDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await chantService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChantDto>> Update(Guid id, UpdateChantRequest request, CancellationToken cancellationToken)
    {
        var result = await chantService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var error = await chantService.DeleteAsync(id, cancellationToken);
        return error is null ? NoContent() : FromError(error);
    }

    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChantDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await chantService.PublishAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChantDto>> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var result = await chantService.UnpublishAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpPut("{id:guid}/playable")]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChantDto>> SetPlayable(
        Guid id,
        SetPlayableChantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await chantService.SetPlayableAsync(id, request.Playable, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    /// <summary>
    /// Uploads the chant's recording, replacing any previous one.
    /// </summary>
    /// <remarks>
    /// An upload rather than a URL field: letting a caller name the URL would let a
    /// chant point at anything at all. The accepted formats and the content type each
    /// is served back with come from one table, so a format cannot be accepted
    /// without declaring how it is served.
    /// </remarks>
    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpPost("{id:guid}/audio")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(MaxChantAudioBytes)]
    public async Task<ActionResult<ChantDto>> UploadAudio(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return FromError(Error.Validation("An audio file is required."));
        }

        if (file.Length > MaxChantAudioBytes)
        {
            return FromError(Error.Validation("The recording must be 15 MB or smaller."));
        }

        if (!PronunciationAudioFormats.ExtensionsByUploadContentType.TryGetValue(file.ContentType, out var extension))
        {
            return FromError(Error.Validation(
                $"Unsupported audio type '{file.ContentType}'. Use MP3, WAV, OGG, WebM, or M4A."));
        }

        await using var stream = file.OpenReadStream();
        var result = await chantService.UploadAudioAsync(id, stream, extension, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }

    [Authorize(Policy = AuthPolicies.ChantsEdit)]
    [HttpDelete("{id:guid}/audio")]
    [ProducesResponseType(typeof(ChantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChantDto>> RemoveAudio(Guid id, CancellationToken cancellationToken)
    {
        var result = await chantService.RemoveAudioAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : FromError(result.Error!);
    }
}
