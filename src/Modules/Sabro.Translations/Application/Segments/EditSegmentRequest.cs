namespace Sabro.Translations.Application.Segments;

public sealed record EditSegmentRequest(
    Guid SegmentId,
    string NewContent);
