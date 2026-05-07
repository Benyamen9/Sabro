using Sabro.Shared.Search;

namespace Sabro.Translations.Application.Search;

internal sealed class SegmentIndexDescriptor : ISearchIndexDescriptor<SegmentSearchDocument>
{
    private static readonly string[] Searchable = { "content" };

    private static readonly string[] Filterable =
    {
        "sourceId",
        "chapterNumber",
        "verseNumber",
        "textVersionId",
        "version",
    };

    public string IndexName => "translations";

    public string PrimaryKey => "id";

    public IndexSettings Settings => new(
        SearchableAttributes: Searchable,
        FilterableAttributes: Filterable,
        Synonyms: new Dictionary<string, IReadOnlyList<string>>());
}
