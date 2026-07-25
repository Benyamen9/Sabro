using System.Collections.Generic;

namespace Sabro.Shared.Search;

/// <summary>
/// Search-engine-agnostic description of how an index should be configured.
/// Translated to the underlying engine's settings shape by the infrastructure layer.
/// </summary>
public sealed record IndexSettings(
    IReadOnlyList<string> SearchableAttributes,
    IReadOnlyList<string> FilterableAttributes,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Synonyms,
    IReadOnlyList<string>? SortableAttributes = null)
{
    public static IndexSettings Empty { get; } = new(
        SearchableAttributes: Array.Empty<string>(),
        FilterableAttributes: Array.Empty<string>(),
        Synonyms: new Dictionary<string, IReadOnlyList<string>>(),
        SortableAttributes: Array.Empty<string>());
}
