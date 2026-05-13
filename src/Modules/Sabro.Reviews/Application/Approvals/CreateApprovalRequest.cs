using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.Approvals;

/// <summary>
/// Body for creating a new approval row. The set of required locator fields
/// depends on <see cref="TargetType"/>:
/// <list type="bullet">
/// <item><c>Segment</c> — <see cref="SourceId"/>, <see cref="ChapterNumber"/>, <see cref="VerseNumber"/>, and <see cref="Version"/> are required; <see cref="AnnotationId"/> must be null.</item>
/// <item><c>Chapter</c> — <see cref="SourceId"/> and <see cref="ChapterNumber"/> are required; <see cref="VerseNumber"/>, <see cref="Version"/>, and <see cref="AnnotationId"/> must all be null.</item>
/// <item><c>Annotation</c> — only <see cref="AnnotationId"/> is required; the service resolves the parent Segment via <c>IAnnotationLookupService</c> and denormalizes the locator on the row, so the other fields must be null.</item>
/// </list>
/// The deciding user is taken from the authenticated principal — callers
/// cannot impersonate via this payload.
/// </summary>
public sealed record CreateApprovalRequest(
    ApprovalTargetType TargetType,
    Guid? SourceId,
    int? ChapterNumber,
    int? VerseNumber,
    int? Version,
    Guid? AnnotationId,
    ApprovalStatus Status,
    string? Note = null);
