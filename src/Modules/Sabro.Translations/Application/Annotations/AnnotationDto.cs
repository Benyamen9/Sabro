namespace Sabro.Translations.Application.Annotations;

public sealed record AnnotationDto(
    Guid Id,
    Guid SegmentId,
    int AnchorStart,
    int AnchorEnd,
    string Body,
    int Version,
    Guid? PreviousVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
