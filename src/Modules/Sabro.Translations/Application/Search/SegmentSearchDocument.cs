using System.Text.Json.Serialization;

namespace Sabro.Translations.Application.Search;

/// <summary>
/// Search projection of a <see cref="Domain.Segment"/>. The index always
/// reflects the latest version of each segment — when a new version is
/// created, the previous version's document is deleted and the new version
/// is upserted under its own id. Older versions remain in Postgres for
/// audit but are not searchable.
/// </summary>
public sealed record SegmentSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("sourceId")]
    public string SourceId { get; init; } = string.Empty;

    [JsonPropertyName("chapterNumber")]
    public int ChapterNumber { get; init; }

    [JsonPropertyName("verseNumber")]
    public int VerseNumber { get; init; }

    [JsonPropertyName("textVersionId")]
    public string TextVersionId { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("createdAtUnix")]
    public long CreatedAtUnix { get; init; }
}
