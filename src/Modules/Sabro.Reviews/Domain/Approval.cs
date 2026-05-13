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
        Guid? annotationId,
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
        AnnotationId = annotationId;
        Status = status;
        DecisionByLogtoUserId = decisionByLogtoUserId;
        Note = note;
    }

    public ApprovalTargetType TargetType { get; private set; }

    public Guid SourceId { get; private set; }

    public int ChapterNumber { get; private set; }

    /// <summary>Verse number for Segment and Annotation targets; null for Chapter.</summary>
    public int? VerseNumber { get; private set; }

    /// <summary>
    /// Version of the target at decision time. Segment: the Segment's own
    /// Version. Annotation: the Annotation's own Version (the parent Segment's
    /// version is implied via the denormalized locator but not pinned here).
    /// Null for Chapter — the chapter itself is not a versioned aggregate.
    /// When the target is edited the new version has no approval until
    /// re-approved; the frontend can surface staleness via this value.
    /// </summary>
    public int? Version { get; private set; }

    /// <summary>The annotation identifier when <see cref="TargetType"/> is Annotation; null otherwise.</summary>
    public Guid? AnnotationId { get; private set; }

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
            annotationId: null,
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
            annotationId: null,
            status,
            decisionByLogtoUserId,
            note);

    /// <summary>
    /// Creates an annotation-targeted approval. The locator fields are
    /// denormalized from the parent Segment by the service (via the cross-
    /// module <c>IAnnotationLookupService</c>) so chapter-scoped list and
    /// effective queries include annotation rows alongside Segment/Chapter
    /// rows. <paramref name="version"/> is the Annotation's own version,
    /// not the parent Segment's.
    /// </summary>
    public static Result<Approval> CreateAnnotation(
        Guid annotationId,
        int version,
        Guid sourceId,
        int chapterNumber,
        int verseNumber,
        ApprovalStatus status,
        string decisionByLogtoUserId,
        string? note = null)
    {
        if (annotationId == Guid.Empty)
        {
            return Result<Approval>.Failure(Error.Validation("AnnotationId is required."));
        }

        if (verseNumber < 1)
        {
            return Result<Approval>.Failure(Error.Validation("VerseNumber must be 1 or greater."));
        }

        if (version < 1)
        {
            return Result<Approval>.Failure(Error.Validation("Version must be 1 or greater."));
        }

        return CreateCore(
            ApprovalTargetType.Annotation,
            sourceId,
            chapterNumber,
            verseNumber,
            version,
            annotationId,
            status,
            decisionByLogtoUserId,
            note);
    }

    private static Result<Approval> CreateCore(
        ApprovalTargetType targetType,
        Guid sourceId,
        int chapterNumber,
        int? verseNumber,
        int? version,
        Guid? annotationId,
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
            annotationId,
            status,
            trimmedDecidedBy,
            normalizedNote));
    }
}
