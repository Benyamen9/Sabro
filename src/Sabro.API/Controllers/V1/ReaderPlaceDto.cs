namespace Sabro.API.Controllers.V1;

/// <summary>The red ribbon in one book: the item last opened there, and when.</summary>
public sealed record ReaderPlaceDto(string Book, string ItemKey, DateTimeOffset OpenedAt, ReaderItemLabelDto? Label);
