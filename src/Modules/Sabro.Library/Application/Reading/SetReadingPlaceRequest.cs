namespace Sabro.Library.Application.Reading;

/// <summary>
/// Moves the ribbon in one book to <paramref name="ItemKey"/>. <paramref name="OpenedAt"/> is sent when
/// a place kept on the device while signed out goes up later; left out, it means "now".
/// </summary>
public sealed record SetReadingPlaceRequest(string ItemKey, DateTimeOffset? OpenedAt = null);
