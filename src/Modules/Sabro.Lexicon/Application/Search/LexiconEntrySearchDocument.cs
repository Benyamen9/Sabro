using System.Text.Json.Serialization;

namespace Sabro.Lexicon.Application.Search;

/// <summary>
/// Flat projection of a <see cref="Domain.LexiconEntry"/> shaped for Meilisearch.
/// The relational store remains the source of truth — this document is rebuilt
/// from Postgres on every write and can be regenerated end-to-end at any time.
/// Property names are explicitly camelCased so JSON serialization is independent
/// of any global serializer configuration.
/// </summary>
public sealed record LexiconEntrySearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("syriacUnvocalized")]
    public string SyriacUnvocalized { get; init; } = string.Empty;

    [JsonPropertyName("syriacVocalized")]
    public string? SyriacVocalized { get; init; }

    [JsonPropertyName("sblTransliteration")]
    public string SblTransliteration { get; init; } = string.Empty;

    [JsonPropertyName("transliterationVariants")]
    public IReadOnlyList<string> TransliterationVariants { get; init; } = Array.Empty<string>();

    [JsonPropertyName("rootId")]
    public string? RootId { get; init; }

    [JsonPropertyName("rootForm")]
    public string? RootForm { get; init; }

    [JsonPropertyName("grammaticalCategory")]
    public string GrammaticalCategory { get; init; } = string.Empty;

    [JsonPropertyName("morphology")]
    public string? Morphology { get; init; }

    [JsonPropertyName("meaningTexts")]
    public IReadOnlyList<string> MeaningTexts { get; init; } = Array.Empty<string>();

    [JsonPropertyName("meaningLanguages")]
    public IReadOnlyList<string> MeaningLanguages { get; init; } = Array.Empty<string>();

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("playableInMeltho")]
    public bool PlayableInMeltho { get; init; }

    [JsonPropertyName("playableLength")]
    public int PlayableLength { get; init; }

    [JsonPropertyName("createdAtUnix")]
    public long CreatedAtUnix { get; init; }

    [JsonPropertyName("updatedAtUnix")]
    public long UpdatedAtUnix { get; init; }

    [JsonPropertyName("hasPronunciationAudio")]
    public bool HasPronunciationAudio { get; init; }

    [JsonPropertyName("pronunciationAudioUrl")]
    public string? PronunciationAudioUrl { get; init; }
}
