using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Translations.Application.Sources;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sources")]
[Authorize(Policy = AuthPolicies.Write)]
public sealed class SourcesController : ApiControllerBase
{
    private readonly ISourceService sourceService;

    public SourcesController(ISourceService sourceService)
    {
        this.sourceService = sourceService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SourceDto>> Create(CreateSourceRequest request, CancellationToken cancellationToken)
    {
        var result = await sourceService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Created($"/api/v1/sources/{result.Value!.Id}", result.Value);
    }
}
