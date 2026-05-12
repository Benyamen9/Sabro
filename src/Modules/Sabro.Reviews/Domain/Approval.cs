using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Reviews.Domain;

/// <summary>
/// An Owner's verdict on a verse-level segment version or a whole chapter.
/// Each decision appends a new row — history is preserved and the latest
/// row by <c>DecisionAt</c> wins. Cascade from chapter to verse is computed
/// at read time: per-verse rows override the chapter verdict when present.
/// </summary>
public sealed class Approval : Entity<Guid>, IAggregateRoot
{
    private Approval(
        ApprovalTargetType targetType,
        Guid sourceId,
        int chapterNumber,
        int? verseNumber,
        int? version,
        ApprovalStatus status,
        string decisionByLogtoUserId,
        string? note)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        DecisionAt = now;
        TargetType = targetType;
        SourceId = sourceId;
        ChapterNumber = chapterNumber;
        VerseNumber = verseNumber;
        Version = version;
        Status = status;
        DecisionByLogtoUserId = decisionByLogtoUserId;
        Note = note;
    }

    public ApprovalTargetType TargetType { get; private set; }

    public Guid SourceId { get; private set; }

    public int ChapterNumber { get; private set; }

    /// <summary>Verse number for <see cref="ApprovalTargetType.Segment"/>; null for chapter approvals.</summary>
    public int? VerseNumber { get; private set; }

    /// <summary>
    /// Version of the Segment the approval was made against. Null for chapter
    /// approvals (the chapter itself is not a versioned aggregate). When a
    /// Segment is edited the new version has no approval until re-approved;
    /// the frontend can surface the staleness using this value.
    /// </summary>
    public int? Version { get; private set; }

    public ApprovalStatus Status { get; private set; }

    public string DecisionByLogtoUserId { get; private set; }

    public DateTimeOffset DecisionAt { get; private set; }

    public string? Note { get; private set; }

    public static Result<Approval> CreateSegment(
        Guid sourceId,
        int chapterNumber,
        int verseNumber,
        int version,
        ApprovalStatus status,
        string decisionByLogtoUserId,
        string? note = null)
    {
        if (verseNumber < 1)
        {
            return Result<Approval>.Failure(Error.Validation("VerseNumber must be 1 or greater."));
        }

        if (version < 1)
        {
            return Result<Approval>.Failure(Error.Validation("Version must be 1 or greater."));
        }

        return CreateCore(
            ApprovalTargetType.Segment,
            sourceId,
            chapterNumber,
            verseNumber,
            version,
            status,
            decisionByLogtoUserId,
            note);
    }

    public static Result<Approval> CreateChapter(
        Guid sourceId,
        int chapterNumber,
        ApprovalStatus status,
        string decisionByLogtoUserId,
        string? note = null) =>
        CreateCore(
            ApprovalTargetType.Chapter,
            sourceId,
            chapterNumber,
            verseNumber: null,
            version: null,
            status,
            decisionByLogtoUserId,
            note);

    private static Result<Approval> CreateCore(
        ApprovalTargetType targetType,
        Guid sourceId,
        int chapterNumber,
        int? verseNumber,
        int? version,
        ApprovalStatus status,
        string decisionByLogtoUserId,
        string? note)
    {
        if (sourceId == Guid.Empty)
        {
            return Result<Approval>.Failure(Error.Validation("SourceId is required."));
        }

        if (chapterNumber < 1)
        {
            return Result<Approval>.Failure(Error.Validation("ChapterNumber must be 1 or greater."));
        }

        if (!Enum.IsDefined(status))
        {
            return Result<Approval>.Failure(Error.Validation("Status is invalid."));
        }

        var trimmedDecidedBy = (decisionByLogtoUserId ?? string.Empty).Trim();
        if (trimmedDecidedBy.Length == 0)
        {
            return Result<Approval>.Failure(Error.Validation("DecisionByLogtoUserId is required."));
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        return Result<Approval>.Success(new Approval(
            targetType,
            sourceId,
            chapterNumber,
            verseNumber,
            version,
            status,
            trimmedDecidedBy,
            normalizedNote));
    }
}
