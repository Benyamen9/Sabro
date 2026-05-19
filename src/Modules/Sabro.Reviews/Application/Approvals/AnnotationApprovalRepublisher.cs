using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sabro.Reviews.Domain;
using Sabro.Reviews.Infrastructure;
using Sabro.Translations.Application.Annotations;

namespace Sabro.Reviews.Application.Approvals;

internal sealed class AnnotationApprovalRepublisher : IAnnotationApprovalRepublisher
{
    private readonly ReviewsDbContext dbContext;
    private readonly IAnnotationApprovalIndexer annotationApprovalIndexer;
    private readonly ILogger<AnnotationApprovalRepublisher> logger;

    public AnnotationApprovalRepublisher(
        ReviewsDbContext dbContext,
        IAnnotationApprovalIndexer annotationApprovalIndexer,
        ILogger<AnnotationApprovalRepublisher> logger)
    {
        this.dbContext = dbContext;
        this.annotationApprovalIndexer = annotationApprovalIndexer;
        this.logger = logger;
    }

    public async Task<int> RepublishAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Approvals
            .AsNoTracking()
            .Where(a => a.TargetType == ApprovalTargetType.Annotation && a.AnnotationId != null)
            .OrderByDescending(a => a.DecisionAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);

        var latestPerAnnotation = rows
            .GroupBy(a => a.AnnotationId!.Value)
            .Select(g => g.First())
            .ToList();

        foreach (var approval in latestPerAnnotation)
        {
            await annotationApprovalIndexer.UpdateApprovalStatusAsync(
                approval.AnnotationId!.Value,
                ToAnnotationApprovalStatus(approval.Status),
                cancellationToken);
        }

        logger.LogInformation(
            "Annotation approval statuses republished. AnnotationCount={Count}",
            latestPerAnnotation.Count);

        return latestPerAnnotation.Count;
    }

    private static AnnotationApprovalStatus ToAnnotationApprovalStatus(ApprovalStatus status) =>
        status switch
        {
            ApprovalStatus.Approved => AnnotationApprovalStatus.Approved,
            ApprovalStatus.Rejected => AnnotationApprovalStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unhandled ApprovalStatus."),
        };
}
