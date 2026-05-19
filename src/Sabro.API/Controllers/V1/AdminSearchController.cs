using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sabro.API.Configuration;
using Sabro.Identity.Application.UserProfiles;
using Sabro.Identity.Domain;
using Sabro.Reviews.Application.Approvals;
using Sabro.Shared.Results;
using Sabro.Shared.Search;

namespace Sabro.API.Controllers.V1;

/// <summary>
/// Owner-only admin surface for rebuilding Meilisearch indexes from PostgreSQL.
/// Per CLAUDE.md, search indexes are not backed up — they are rebuilt on demand
/// from the relational source of truth. Each registered <see cref="ISearchRebuilder"/>
/// owns one index; the dispatcher matches by <see cref="ISearchRebuilder.IndexName"/>.
/// Annotation approval statuses are not part of the annotation rebuild (the
/// rebuilder writes <c>approvalStatus = null</c>); the operator runs
/// <c>republish-annotation-approvals</c> afterward to refill them from Reviews.
/// </summary>
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/search")]
public sealed class AdminSearchController : ApiControllerBase
{
    private readonly IEnumerable<ISearchRebuilder> rebuilders;
    private readonly IAnnotationApprovalRepublisher annotationApprovalRepublisher;
    private readonly IUserProfileService userProfiles;
    private readonly ILogger<AdminSearchController> logger;

    public AdminSearchController(
        IEnumerable<ISearchRebuilder> rebuilders,
        IAnnotationApprovalRepublisher annotationApprovalRepublisher,
        IUserProfileService userProfiles,
        ILogger<AdminSearchController> logger)
    {
        this.rebuilders = rebuilders;
        this.annotationApprovalRepublisher = annotationApprovalRepublisher;
        this.userProfiles = userProfiles;
        this.logger = logger;
    }

    [HttpPost("rebuild/{indexName}")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(SearchRebuildResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SearchRebuildResponse>> Rebuild(string indexName, CancellationToken cancellationToken)
    {
        var ownerCheck = await EnsureOwnerAsync(cancellationToken);
        if (ownerCheck is not null)
        {
            return ownerCheck;
        }

        var rebuilder = rebuilders.FirstOrDefault(r => string.Equals(r.IndexName, indexName, StringComparison.Ordinal));
        if (rebuilder is null)
        {
            return FromError(Error.NotFound($"Search index '{indexName}' is not registered."));
        }

        var result = await rebuilder.RebuildAsync(cancellationToken);
        logger.LogInformation(
            "Search index rebuild completed. Index={IndexName} DocumentCount={Count} ElapsedMs={Elapsed}",
            indexName,
            result.DocumentCount,
            result.Elapsed.TotalMilliseconds);

        return Ok(new SearchRebuildResponse(indexName, result.DocumentCount, result.Elapsed));
    }

    [HttpPost("republish-annotation-approvals")]
    [Authorize(Policy = AuthPolicies.Write)]
    [ProducesResponseType(typeof(AnnotationApprovalRepublishResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnnotationApprovalRepublishResponse>> RepublishAnnotationApprovals(CancellationToken cancellationToken)
    {
        var ownerCheck = await EnsureOwnerAsync(cancellationToken);
        if (ownerCheck is not null)
        {
            return ownerCheck;
        }

        var count = await annotationApprovalRepublisher.RepublishAsync(cancellationToken);
        logger.LogInformation(
            "Annotation approval republish completed. AnnotationCount={Count}",
            count);
        return Ok(new AnnotationApprovalRepublishResponse(count));
    }

    private async Task<ActionResult?> EnsureOwnerAsync(CancellationToken cancellationToken)
    {
        var logtoUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(logtoUserId))
        {
            return FromError(Error.Validation("Authenticated user is missing a sub claim."));
        }

        var profileResult = await userProfiles.GetOrCreateForLogtoUserAsync(logtoUserId, cancellationToken);
        if (!profileResult.IsSuccess)
        {
            return FromError(profileResult.Error!);
        }

        if (profileResult.Value!.Role != Role.Owner)
        {
            return FromError(Error.Forbidden("Only the Owner may invoke admin search endpoints."));
        }

        return null;
    }
}
