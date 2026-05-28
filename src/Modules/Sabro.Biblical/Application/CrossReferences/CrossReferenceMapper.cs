using Sabro.Biblical.Domain;

namespace Sabro.Biblical.Application.CrossReferences;

internal static class CrossReferenceMapper
{
    public static CrossReferenceDto Map(CrossReference reference, BiblicalPassage passage, string bookCode) => new(
        reference.Id,
        reference.AnnotationAnchorId,
        reference.PassageId,
        bookCode,
        passage.ChapterNumber,
        passage.VerseNumber,
        reference.Source,
        reference.Kind,
        reference.CreatedAt,
        reference.UpdatedAt);
}
