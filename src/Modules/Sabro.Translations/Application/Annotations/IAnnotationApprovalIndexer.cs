namespace Sabro.Translations.Application.Annotations;

/// <summary>
/// Cross-module surface that lets Reviews notify the Translations module
/// when the Owner's verdict on an annotation version changes. Translations
/// re-upserts the annotation's search document with the new
/// <see cref="AnnotationApprovalStatus"/> so search clients can filter on
/// approved-only / rejected-only results. Lives next to <see cref="IAnnotationLookupService"/>
/// — both are narrow public surfaces exposed for cross-module callers, with
/// the full annotation CRUD surface kept internal.
/// </summary>
public interface IAnnotationApprovalIndexer
{
    Task UpdateApprovalStatusAsync(
        Guid annotationId,
        AnnotationApprovalStatus status,
        CancellationToken cancellationToken);
}
