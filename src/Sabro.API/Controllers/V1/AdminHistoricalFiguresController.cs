using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Owner-only editorial surface for the Shmo figure roster (the backoffice write
/// path). Gated by the <c>api:v1:admin</c> scope. Unlike client apps, this is part
/// of Sabro itself — it may create, edit, delete, and change the lifecycle of figures.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/historical-figures")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminHistoricalFiguresController : ApiControllerBase
{
    private readonly IHistoricalFigureService figureService;

    public AdminHistoricalFiguresController(IHistoricalFigureService figureService)
    {
        this.figureService = figureService;
    }

    [Authorize(Policy = AuthPolicies.FiguresEdit)]
    [HttpPost]
    [ProducesResponseType(typeof(HistoricalFigureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HistoricalFigureDto>> Create(CreateHistoricalFigureRequest request, CancellationToken cancellationToken)
    {
        var result = await figureService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    /// <summary>
    /// Lists roster figures for the backoffice — Draft and Published alike, unlike the
    /// public roster. Backed by a direct relational query: the roster is a few hundred
    /// rows at most, so a dedicated search index would be plumbing without payoff.
    /// </summary>
    [Authorize(Policy = AuthPolicies.FiguresView)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<HistoricalFigureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<HistoricalFigureDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] HistoricalFigureStatus? status = null,
        [FromQuery] HistoricalFigureCategory? category = null,
        [FromQuery] HistoricalPeriod? period = null,
        [FromQuery] HistoricalFigureRole? role = null,
        [FromQuery] HistoricalFigureRegion? region = null,
        [FromQuery] bool? playableInShmo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await figureService.ListAsync(
            search,
            status,
            category,
            period,
            role,
            region,
            playableInShmo,
            page,
            pageSize,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = AuthPolicies.FiguresView)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HistoricalFigureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoricalFigureDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await figureService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = AuthPolicies.FiguresEdit)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(HistoricalFigureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoricalFigureDto>> Update(Guid id, UpdateHistoricalFigureRequest request, CancellationToken cancellationToken)
    {
        var result = await figureService.UpdateAsync(id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = AuthPolicies.FiguresEdit)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var error = await figureService.DeleteAsync(id, cancellationToken);
        if (error is not null)
        {
            return FromError(error);
        }

        return NoContent();
    }

    [Authorize(Policy = AuthPolicies.FiguresEdit)]
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(HistoricalFigureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoricalFigureDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await figureService.PublishAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = AuthPolicies.FiguresEdit)]
    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(HistoricalFigureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoricalFigureDto>> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var result = await figureService.UnpublishAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [Authorize(Policy = AuthPolicies.FiguresEdit)]
    [HttpPut("{id:guid}/playable")]
    [ProducesResponseType(typeof(HistoricalFigureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HistoricalFigureDto>> SetPlayable(Guid id, SetPlayableHistoricalFigureRequest request, CancellationToken cancellationToken)
    {
        var result = await figureService.SetPlayableAsync(id, request.Playable, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
