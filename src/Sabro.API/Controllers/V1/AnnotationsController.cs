using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Shared.Pagination;
using Sabro.Translations.Application.Annotations;
using Sabro.Translations.Application.Search;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/annotations")]
public sealed class AnnotationsController : ApiControllerBase
{
    private readonly IAnnotationService annotationService;
    private readonly IAnnotationSearchService searchService;

    public AnnotationsController(
        IAnnotationService annotationService,
        IAnnotationSearchService searchService)
    {
        this.annotationService = annotationService;
        this.searchService = searchService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(AnnotationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnotationDto>> Create(CreateAnnotationRequest request, CancellationToken cancellationToken)
    {
        var result = await annotationService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(AnnotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnotationDto>> Edit(Guid id, EditAnnotationBody body, CancellationToken cancellationToken)
    {
        var result = await annotationService.EditAsync(new EditAnnotationRequest(id, body.NewBody), cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(AnnotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnotationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await annotationService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(PagedResult<AnnotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AnnotationDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await annotationService.ListAsync(page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("search")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(PagedResult<AnnotationSearchHitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AnnotationSearchHitDto>>> Search(
        [FromQuery] string? q = null,
        [FromQuery] Guid? segmentId = null,
        [FromQuery] Guid? sourceId = null,
        [FromQuery] int? chapter = null,
        [FromQuery] int? verse = null,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchAsync(q, segmentId, sourceId, chapter, verse, approvalStatus, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
