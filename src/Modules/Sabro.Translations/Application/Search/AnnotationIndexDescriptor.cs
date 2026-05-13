using Sabro.Shared.Search;

namespace Sabro.Translations.Application.Search;

internal sealed class AnnotationIndexDescriptor : ISearchIndexDescriptor<AnnotationSearchDocument>
{
    private static readonly string[] Searchable = { "body" };

    private static readonly string[] Filterable =
    {
        "segmentId",
        "sourceId",
        "chapterNumber",
        "verseNumber",
        "version",
    };

    public string IndexName => "annotations";

    public string PrimaryKey => "id";

    public IndexSettings Settings => new(
        SearchableAttributes: Searchable,
        FilterableAttributes: Filterable,
        Synonyms: new Dictionary<string, IReadOnlyList<string>>());
}
