namespace Sabro.Library.Application.Reading;

/// <summary>An item the reader marked ܀.</summary>
public sealed record ReadingMarkDto(string Book, string ItemKey, DateTimeOffset MarkedAt);
