using System.Text.Json.Serialization;

namespace Sabro.Biblical.Application.Search;

/// <summary>
/// Flat projection of a <see cref="Domain.BiblicalPassage"/> shaped for
/// Meilisearch. Book metadata is denormalized onto every passage so that a
/// search like "matthew 3" matches passage rows directly, without a join
/// against <see cref="Domain.BiblicalBook"/> at query time. Property names
/// are explicitly camelCased so JSON serialization is independent of any
/// global serializer configuration.
/// </summary>
public sealed record BiblicalPassageSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("bookId")]
    public string BookId { get; init; } = string.Empty;

    [JsonPropertyName("bookCode")]
    public string BookCode { get; init; } = string.Empty;

    [JsonPropertyName("bookEnglishName")]
    public string BookEnglishName { get; init; } = string.Empty;

    [JsonPropertyName("bookSyriacName")]
    public string? BookSyriacName { get; init; }

    [JsonPropertyName("testament")]
    public string Testament { get; init; } = string.Empty;

    [JsonPropertyName("bookOrder")]
    public int BookOrder { get; init; }

    [JsonPropertyName("chapterNumber")]
    public int ChapterNumber { get; init; }

    [JsonPropertyName("verseNumber")]
    public int VerseNumber { get; init; }

    /// <summary>
    /// Derived display string in the form <c>{EnglishName} {chapter}:{verse}</c>
    /// (e.g. <c>Matthew 3:7</c>). Stored so that full-text queries against the
    /// rendered reference work, and so clients can render a result row without
    /// needing to interpolate.
    /// </summary>
    [JsonPropertyName("reference")]
    public string Reference { get; init; } = string.Empty;
}
