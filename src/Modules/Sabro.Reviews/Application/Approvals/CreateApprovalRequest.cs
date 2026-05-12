using Sabro.Reviews.Domain;

namespace Sabro.Reviews.Application.Approvals;

/// <summary>
/// Body for creating a new approval row. <see cref="VerseNumber"/> and
/// <see cref="Version"/> are required for <see cref="ApprovalTargetType.Segment"/>
/// and must be omitted for <see cref="ApprovalTargetType.Chapter"/>.
/// The deciding user is taken from the authenticated principal — callers
/// cannot impersonate via this payload.
/// </summary>
public sealed record CreateApprovalRequest(
    ApprovalTargetType TargetType,
    Guid SourceId,
    int ChapterNumber,
    int? VerseNumber,
    int? Version,
    ApprovalStatus Status,
    string? Note = null);
