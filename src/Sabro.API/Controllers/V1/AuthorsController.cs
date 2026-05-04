using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Translations.Application.Authors;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/authors")]
[Authorize(Policy = AuthPolicies.Write)]
public sealed class AuthorsController : ApiControllerBase
{
    private readonly IAuthorService authorService;

    public AuthorsController(IAuthorService authorService)
    {
        this.authorService = authorService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AuthorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthorDto>> Create(CreateAuthorRequest request, CancellationToken cancellationToken)
    {
        var result = await authorService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Created($"/api/v1/authors/{result.Value!.Id}", result.Value);
    }
}
