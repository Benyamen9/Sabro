using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Biblical.Application.Books;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/biblical-books")]
public sealed class BiblicalBooksController : ApiControllerBase
{
    private readonly IBiblicalBookService bookService;

    public BiblicalBooksController(IBiblicalBookService bookService)
    {
        this.bookService = bookService;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(BiblicalBookDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BiblicalBookDto>> Create(CreateBiblicalBookRequest request, CancellationToken cancellationToken)
    {
        var result = await bookService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(BiblicalBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BiblicalBookDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await bookService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("by-code/{code}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(BiblicalBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BiblicalBookDto>> GetByCode(string code, CancellationToken cancellationToken)
    {
        var result = await bookService.GetByCodeAsync(code, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(PagedResult<BiblicalBookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BiblicalBookDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await bookService.ListAsync(page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
