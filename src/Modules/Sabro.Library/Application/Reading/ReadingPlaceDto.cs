namespace Sabro.Library.Application.Reading;

/// <summary>Where the reader last was in one book (the red ribbon).</summary>
public sealed record ReadingPlaceDto(string Book, string ItemKey, DateTimeOffset OpenedAt);
