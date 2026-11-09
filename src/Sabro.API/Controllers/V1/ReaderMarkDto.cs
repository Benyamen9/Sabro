namespace Sabro.API.Controllers.V1;

/// <summary>An item marked ܀, and when.</summary>
public sealed record ReaderMarkDto(string Book, string ItemKey, DateTimeOffset MarkedAt, ReaderItemLabelDto? Label);
