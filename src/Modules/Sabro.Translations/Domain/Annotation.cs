using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Translations.Domain;

public sealed class Annotation : Entity<Guid>, IAggregateRoot
{
    private Annotation(
        Guid segmentId,
        int anchorStart,
        int anchorEnd,
        string body,
        int version,
        Guid? previousVersionId)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        SegmentId = segmentId;
        AnchorStart = anchorStart;
        AnchorEnd = anchorEnd;
        Body = body;
        Version = version;
        PreviousVersionId = previousVersionId;
    }

    public Guid SegmentId { get; private set; }

    public int AnchorStart { get; private set; }

    public int AnchorEnd { get; private set; }

    public string Body { get; private set; }

    public int Version { get; private set; }

    public Guid? PreviousVersionId { get; private set; }

    public static Result<Annotation> Create(Guid segmentId, int anchorStart, int anchorEnd, string body)
    {
        if (segmentId == Guid.Empty)
        {
            return Result<Annotation>.Failure(Error.Validation("SegmentId is required."));
        }

        if (anchorStart < 0)
        {
            return Result<Annotation>.Failure(Error.Validation("AnchorStart must be 0 or greater."));
        }

        if (anchorEnd <= anchorStart)
        {
            return Result<Annotation>.Failure(Error.Validation("AnchorEnd must be greater than AnchorStart."));
        }

        var trimmedBody = (body ?? string.Empty).Trim();
        if (trimmedBody.Length == 0)
        {
            return Result<Annotation>.Failure(Error.Validation("Body is required."));
        }

        return Result<Annotation>.Success(new Annotation(
            segmentId,
            anchorStart,
            anchorEnd,
            trimmedBody,
            version: 1,
            previousVersionId: null));
    }

    public Result<Annotation> CreateNextVersion(string newBody)
    {
        var trimmedBody = (newBody ?? string.Empty).Trim();
        if (trimmedBody.Length == 0)
        {
            return Result<Annotation>.Failure(Error.Validation("Body is required."));
        }

        return Result<Annotation>.Success(new Annotation(
            SegmentId,
            AnchorStart,
            AnchorEnd,
            trimmedBody,
            version: Version + 1,
            previousVersionId: Id));
    }
}
