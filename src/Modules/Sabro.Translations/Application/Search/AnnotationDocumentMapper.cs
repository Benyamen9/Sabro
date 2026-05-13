using Sabro.Translations.Application.Annotations;
using Sabro.Translations.Domain;

namespace Sabro.Translations.Application.Search;

internal static class AnnotationDocumentMapper
{
    public static AnnotationSearchDocument Map(
        Annotation annotation,
        Segment segment,
        AnnotationApprovalStatus? approvalStatus = null) => new()
        {
            Id = annotation.Id.ToString("D"),
            SegmentId = segment.Id.ToString("D"),
            SourceId = segment.SourceId.ToString("D"),
            ChapterNumber = segment.ChapterNumber,
            VerseNumber = segment.VerseNumber,
            AnchorStart = annotation.AnchorStart,
            AnchorEnd = annotation.AnchorEnd,
            Body = annotation.Body,
            Version = annotation.Version,
            ApprovalStatus = approvalStatus switch
            {
                AnnotationApprovalStatus.Approved => "approved",
                AnnotationApprovalStatus.Rejected => "rejected",
                _ => null,
            },
            CreatedAtUnix = annotation.CreatedAt.ToUnixTimeSeconds(),
        };
}
