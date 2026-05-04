using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Translations.Application.Sources;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sources")]
public sealed class SourcesController : ApiControllerBase
{
    private readonly ISourceService sourceService;

    public SourcesController(ISourceService sourceService)
    {
        this.sourceService = sourceService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SourceDto>> Create(CreateSourceRequest request, CancellationToken cancellationToken)
    {
        var result = await sourceService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SourceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sourceService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
