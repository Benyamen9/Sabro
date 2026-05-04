using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Translations.Application.Segments;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/segments")]
[Authorize(Policy = AuthPolicies.Write)]
public sealed class SegmentsController : ApiControllerBase
{
    private readonly ISegmentService segmentService;

    public SegmentsController(ISegmentService segmentService)
    {
        this.segmentService = segmentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SegmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SegmentDto>> Create(CreateSegmentRequest request, CancellationToken cancellationToken)
    {
        var result = await segmentService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Created($"/api/v1/segments/{result.Value!.Id}", result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SegmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SegmentDto>> Edit(Guid id, EditSegmentBody body, CancellationToken cancellationToken)
    {
        var result = await segmentService.EditAsync(new EditSegmentRequest(id, body.NewContent), cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
