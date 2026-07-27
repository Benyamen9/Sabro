using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.Historical.Application.Figures;
using Sabro.Historical.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// The public figure roster — every published historical figure, browsable by
/// anyone. Anonymous like the dictionary and the Meltho library (public,
/// non-personal content; still rate-limited): Shmo's guess-search reads this
/// without an account. The payloads never carry editorial state or the playable
/// flag, so the puzzle pool cannot be enumerated from here.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/historical-figures")]
public sealed class HistoricalFiguresController : ApiControllerBase
{
    private readonly IHistoricalFigureService figureService;

    public HistoricalFiguresController(IHistoricalFigureService figureService)
    {
        this.figureService = figureService;
    }

    /// <summary>
    /// Browses published figures alphabetically by name, paged, optionally filtered
    /// by category, role, or region.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<HistoricalFigureListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<HistoricalFigureListItem>>> List(
        [FromQuery] HistoricalFigureCategory? category = null,
        [FromQuery] HistoricalPeriod? period = null,
        [FromQuery] HistoricalFigureRole? role = null,
        [FromQuery] HistoricalFigureRegion? region = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await figureService.ListPublishedAsync(category, period, role, region, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    /// <summary>One published figure and its attributes. 404 for drafts and unknown ids alike.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HistoricalFigureListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoricalFigureListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await figureService.GetPublishedByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
