namespace Sabro.Library.Application.Reading;

/// <summary>
/// Optional body for marking an item. <paramref name="MarkedAt"/> keeps the date of a mark made on the
/// device while signed out; left out, it means "now".
/// </summary>
public sealed record MarkItemRequest(DateTimeOffset? MarkedAt = null);
