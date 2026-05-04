namespace Sabro.Translations.Application.Annotations;

public sealed record CreateAnnotationRequest(
    Guid SegmentId,
    int AnchorStart,
    int AnchorEnd,
    string Body);
