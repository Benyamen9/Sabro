namespace Sabro.Translations.Application.Annotations;

/// <summary>
/// Owner's verdict on an annotation version, communicated to the Translations
/// module by Reviews via <see cref="IAnnotationApprovalIndexer"/>. Mirrors the
/// shape of <c>Reviews.Domain.ApprovalStatus</c> on the cross-module boundary
/// without taking a project reference on Reviews.
/// </summary>
public enum AnnotationApprovalStatus
{
    Approved,
    Rejected,
}
