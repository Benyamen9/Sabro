using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sabro.API.Configuration;
using Sabro.Reviews.Application.Approvals;
using Sabro.Reviews.Domain;
using Sabro.Shared.Pagination;
using Sabro.Shared.Results;

namespace Sabro.API.Controllers.V1;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/approvals")]
public sealed class ApprovalsController : ApiControllerBase
{
    private readonly IApprovalService service;

    public ApprovalsController(IApprovalService service)
    {
        this.service = service;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(ApprovalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApprovalDto>> Create(CreateApprovalRequest request, CancellationToken cancellationToken)
    {
        var subResult = ResolveLogtoUserId();
        if (!subResult.IsSuccess)
        {
            return FromError(subResult.Error!);
        }

        var result = await service.CreateAsync(request, subResult.Value!, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(ApprovalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalDto>> GetById(Guid id, CancellationToken cancellationToken)
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
    [ProducesResponseType(typeof(PagedResult<ApprovalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ApprovalDto>>> List(
        [FromQuery] ApprovalTargetType? targetType = null,
        [FromQuery] ApprovalStatus? status = null,
        [FromQuery] Guid? sourceId = null,
        [FromQuery] int? chapter = null,
        [FromQuery] int? verse = null,
        [FromQuery] Guid? annotationId = null,
        [FromQuery] string? decidedBy = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var filters = new ApprovalListFilters(
            TargetType: targetType,
            Status: status,
            SourceId: sourceId,
            ChapterNumber: chapter,
            VerseNumber: verse,
            AnnotationId: annotationId,
            DecisionByLogtoUserId: decidedBy);
        var result = await service.ListAsync(filters, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    [HttpGet("effective")]
    [Authorize(Policy = AuthPolicies.Read)]
    [ProducesResponseType(typeof(EffectiveChapterApprovalsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EffectiveChapterApprovalsDto>> GetEffective(
        [FromQuery] Guid sourceId,
        [FromQuery] int chapter,
        CancellationToken cancellationToken)
    {
        var result = await service.GetEffectiveForChapterAsync(sourceId, chapter, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromError(result.Error!);
        }

        return Ok(result.Value);
    }

    private Result<string> ResolveLogtoUserId()
    {
        var logtoUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(logtoUserId))
        {
            return Result<string>.Failure(Error.Validation("Authenticated user is missing a sub claim."));
        }

        return Result<string>.Success(logtoUserId);
    }
}
