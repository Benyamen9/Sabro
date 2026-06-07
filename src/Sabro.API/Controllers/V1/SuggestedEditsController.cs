using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Reviews.Application.SuggestedEdits;
using Sabro.Reviews.Domain;
using Sabro.Shared.Pagination;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/suggested-edits")]
public sealed class SuggestedEditsController : ApiControllerBase
{
    private readonly ISuggestedEditService service;

    public SuggestedEditsController(ISuggestedEditService service)
    {
        this.service = service;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(SuggestedEditDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SuggestedEditDto>> Propose(CreateSuggestedEditRequest request, CancellationToken cancellationToken)
    {
        var subResult = ResolveLogtoUserId();
        if (!subResult.IsSuccess)
        {
            return FromError(subResult.Error!);
        }

        var result = await service.ProposeAsync(request, subResult.Value!, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(SuggestedEditDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuggestedEditDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(PagedResult<SuggestedEditDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SuggestedEditDto>>> List(
        [FromQuery] SuggestedEditStatus? status = null,
        [FromQuery] SuggestedEditTargetType? targetType = null,
        [FromQuery] Guid? targetId = null,
        [FromQuery] string? submittedBy = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var filters = new SuggestedEditListFilters(status, targetType, targetId, submittedBy);
        var result = await service.ListAsync(filters, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/accept")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(SuggestedEditDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult<SuggestedEditDto>> Accept(Guid id, DecisionRequest? request, CancellationToken cancellationToken) =>
        DecideAsync(id, request, accept: true, cancellationToken);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(SuggestedEditDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<ActionResult<SuggestedEditDto>> Reject(Guid id, DecisionRequest? request, CancellationToken cancellationToken) =>
        DecideAsync(id, request, accept: false, cancellationToken);

    private async Task<ActionResult<SuggestedEditDto>> DecideAsync(
        Guid id,
        DecisionRequest? request,
        bool accept,
        CancellationToken cancellationToken)
    {
        var subResult = ResolveLogtoUserId();
        if (!subResult.IsSuccess)
        {
            return FromError(subResult.Error!);
        }

        var body = request ?? new DecisionRequest();
        var result = accept
            ? await service.AcceptAsync(id, body, subResult.Value!, cancellationToken)
            : await service.RejectAsync(id, body, subResult.Value!, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }
}
