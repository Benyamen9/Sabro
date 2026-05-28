using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Application.CrossReferences;

/// <summary>
/// API representation of a typed biblical cross-reference.
/// <para>
/// <see cref="Source"/> and <see cref="Kind"/> serialize as their enum member
/// names (e.g. <c>"Author"</c>, <c>"Quotation"</c>) — part of the
/// <c>/api/v1/</c> contract. Adding new values is backwards-compatible;
/// renaming existing ones is a breaking change for clients.
/// </para>
/// </summary>
public sealed record CrossReferenceDto(
    Guid Id,
    Guid AnnotationAnchorId,
    Guid PassageId,
    string BookCode,
    int ChapterNumber,
    int VerseNumber,
    ReferenceSource Source,
    ReferenceKind Kind,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
