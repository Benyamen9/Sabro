using System.Text.Json.Serialization;

namespace Sabro.Translations.Application.Search;

/// <summary>
/// Search projection of an <see cref="Domain.Annotation"/>. Parent
/// (Source, Chapter, Verse) coordinates are denormalized from the
/// owning <see cref="Domain.Segment"/> so chapter-scoped searches do
/// not need a Segment join at query time. The index always reflects
/// the latest version: when a new version is created the previous
/// version's document is deleted and the new one is upserted under
/// its own id. Older versions remain in Postgres for audit but are
/// not searchable.
/// </summary>
public sealed record AnnotationSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("segmentId")]
    public string SegmentId { get; init; } = string.Empty;

    [JsonPropertyName("sourceId")]
    public string SourceId { get; init; } = string.Empty;

    [JsonPropertyName("chapterNumber")]
    public int ChapterNumber { get; init; }

    [JsonPropertyName("verseNumber")]
    public int VerseNumber { get; init; }

    [JsonPropertyName("anchorStart")]
    public int AnchorStart { get; init; }

    [JsonPropertyName("anchorEnd")]
    public int AnchorEnd { get; init; }

    [JsonPropertyName("body")]
    public string Body { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public int Version { get; init; }

    /// <summary>
    /// Owner's approval verdict on this annotation version, lowercased
    /// (<c>"approved"</c> or <c>"rejected"</c>), or null if no approval row
    /// exists yet. Reset to null when a new version is created via
    /// <see cref="Domain.Annotation.CreateNextVersion(string)"/> because
    /// approval rows are pinned to a specific <see cref="Version"/>.
    /// </summary>
    [JsonPropertyName("approvalStatus")]
    public string? ApprovalStatus { get; init; }

    [JsonPropertyName("createdAtUnix")]
    public long CreatedAtUnix { get; init; }
}
