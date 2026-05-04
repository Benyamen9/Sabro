using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Translations.Application.Annotations;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/annotations")]
public sealed class AnnotationsController : ApiControllerBase
{
    private readonly IAnnotationService annotationService;

    public AnnotationsController(IAnnotationService annotationService)
    {
        this.annotationService = annotationService;
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
}
