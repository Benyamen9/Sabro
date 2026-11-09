namespace Sabro.API.Controllers.V1;

/// <summary>
/// The reader's Library in one response: a place per book, and every mark newest first. Places and marks
/// whose item is no longer published are left out (their rows stay, and return if the item does).
/// </summary>
public sealed record ReaderShelfDto(IReadOnlyList<ReaderPlaceDto> Places, IReadOnlyList<ReaderMarkDto> Marks);
