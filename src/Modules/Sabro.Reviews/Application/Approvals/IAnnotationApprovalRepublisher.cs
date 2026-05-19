namespace Sabro.Reviews.Application.Approvals;

/// <summary>
/// Operator hook that walks every Annotation-targeted Approval row in
/// <c>reviews.approvals</c>, picks the latest verdict per annotation, and
/// pushes the resulting status to the Translations module via the existing
/// <c>IAnnotationApprovalIndexer</c> surface. Used after an annotation search
/// index rebuild — the rebuilder emits documents with <c>approvalStatus = null</c>
/// because approval verdicts live in Reviews, not Translations.
/// </summary>
public interface IAnnotationApprovalRepublisher
{
    Task<int> RepublishAsync(CancellationToken cancellationToken);
}
