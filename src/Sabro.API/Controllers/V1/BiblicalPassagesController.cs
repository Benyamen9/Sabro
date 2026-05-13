using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Biblical.Application.Passages;
using Sabro.Biblical.Application.Search;
using Sabro.Biblical.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/biblical-passages")]
public sealed class BiblicalPassagesController : ApiControllerBase
{
    private readonly IBiblicalPassageService passageService;
    private readonly IBiblicalPassageSearchService searchService;

    public BiblicalPassagesController(
        IBiblicalPassageService passageService,
        IBiblicalPassageSearchService searchService)
    {
        this.passageService = passageService;
        this.searchService = searchService;
    }

    /// <summary>
    /// Idempotent get-or-create. Returns 201 when the passage was just inserted,
    /// 200 when it already existed. The body is identical in both cases so
    /// clients can ignore the distinction unless they care.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(BiblicalPassageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BiblicalPassageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BiblicalPassageDto>> GetOrCreate(GetOrCreateBiblicalPassageRequest request, CancellationToken cancellationToken)
    {
        var result = await passageService.GetOrCreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        var lookup = result.Value!;
        return lookup.WasCreated
            ? CreatedAtAction(nameof(GetById), new { id = lookup.Passage.Id, version = "1" }, lookup.Passage)
            : Ok(lookup.Passage);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(BiblicalPassageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BiblicalPassageDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await passageService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(PagedResult<BiblicalPassageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BiblicalPassageDto>>> List(
        [FromQuery] string? bookCode = null,
        [FromQuery] int? chapter = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await passageService.ListAsync(bookCode, chapter, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("search")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(PagedResult<BiblicalPassageSearchHitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BiblicalPassageSearchHitDto>>> Search(
        [FromQuery] string? q = null,
        [FromQuery] string? bookCode = null,
        [FromQuery] Testament? testament = null,
        [FromQuery] int? chapter = null,
        [FromQuery] int? verse = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchAsync(q, bookCode, testament, chapter, verse, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
