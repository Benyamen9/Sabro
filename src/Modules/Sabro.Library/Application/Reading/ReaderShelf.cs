namespace Sabro.Library.Application.Reading;

/// <summary>Everything the Library keeps for one reader: a place per book read, and every mark, newest first.</summary>
public sealed record ReaderShelf(IReadOnlyList<ReadingPlaceDto> Places, IReadOnlyList<ReadingMarkDto> Marks);
