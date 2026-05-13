using Sabro.Shared.Search;

namespace Sabro.Biblical.Application.Search;

internal sealed class BiblicalPassageIndexDescriptor : ISearchIndexDescriptor<BiblicalPassageSearchDocument>
{
    private static readonly string[] Searchable =
    {
        "bookCode",
        "bookEnglishName",
        "bookSyriacName",
        "reference",
    };

    private static readonly string[] Filterable =
    {
        "bookId",
        "bookCode",
        "testament",
        "chapterNumber",
        "verseNumber",
    };

    public string IndexName => "biblical_passages";

    public string PrimaryKey => "id";

    public IndexSettings Settings => new(
        SearchableAttributes: Searchable,
        FilterableAttributes: Filterable,
        Synonyms: new Dictionary<string, IReadOnlyList<string>>());
}
