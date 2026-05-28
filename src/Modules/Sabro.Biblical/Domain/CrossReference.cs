using Sabro.Shared.Domain;
using Sabro.Shared.Results;

namespace Sabro.Biblical.Domain;

/// <summary>
/// A typed link between an annotation anchor in the commentary and a target
/// <see cref="BiblicalPassage"/>. Typing is independent on two axes:
/// <see cref="Source"/> (who originated the reference — Author vs Editorial)
/// and <see cref="Kind"/> (the nature of the reference — Quotation vs Allusion).
/// </summary>
public sealed class CrossReference : Entity<Guid>, IAggregateRoot
{
    private CrossReference(Guid annotationAnchorId, Guid passageId, ReferenceSource source, ReferenceKind kind)
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        AnnotationAnchorId = annotationAnchorId;
        PassageId = passageId;
        Source = source;
        Kind = kind;
    }

    /// <summary>
    /// Identifier of the annotation anchor in the commentary that this
    /// reference attaches to. Owned by the Translations module; stored here
    /// as a plain <see cref="Guid"/> (no cross-module FK).
    /// </summary>
    public Guid AnnotationAnchorId { get; private set; }

    /// <summary>FK to the target <see cref="BiblicalPassage"/>.</summary>
    public Guid PassageId { get; private set; }

    public ReferenceSource Source { get; private set; }

    public ReferenceKind Kind { get; private set; }

    public static Result<CrossReference> Create(
        Guid annotationAnchorId,
        Guid passageId,
        ReferenceSource source,
        ReferenceKind kind)
    {
        if (annotationAnchorId == Guid.Empty)
        {
            return Result<CrossReference>.Failure(Error.Validation("AnnotationAnchorId is required."));
        }

        if (passageId == Guid.Empty)
        {
            return Result<CrossReference>.Failure(Error.Validation("PassageId is required."));
        }

        if (!Enum.IsDefined(source))
        {
            return Result<CrossReference>.Failure(Error.Validation("Source is invalid."));
        }

        if (!Enum.IsDefined(kind))
        {
            return Result<CrossReference>.Failure(Error.Validation("Kind is invalid."));
        }

        return Result<CrossReference>.Success(new CrossReference(annotationAnchorId, passageId, source, kind));
    }
}
