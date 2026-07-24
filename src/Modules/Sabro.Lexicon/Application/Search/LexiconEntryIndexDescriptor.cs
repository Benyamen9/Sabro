using System.Linq;
using Sabro.Shared.Search;

namespace Sabro.Lexicon.Application.Search;

internal sealed class LexiconEntryIndexDescriptor : ISearchIndexDescriptor<LexiconEntrySearchDocument>
{
    private static readonly string[] Searchable =
    {
        "syriacUnvocalized",
        "syriacVocalized",
        "sblTransliteration",
        "transliterationVariants",
        "rootForm",
        "meaningTexts",
    };

    private static readonly string[] Filterable =
    {
        "grammaticalCategory",
        "rootId",
        "rootForm",
        "meaningLanguages",
        "status",
        "playableInMeltho",
        "playableLength",
        "hasPronunciationAudio",
    };

    private static readonly string[] Sortable =
    {
        "createdAtUnix",
        "syriacUnvocalized",
        "playableLength",
        "status",
    };

    /// <summary>
    /// Seed transliteration synonyms drawn from the example in CLAUDE.md
    /// (<c>meltho ≡ meltā ≡ melthā ≡ meltha</c>). Each member of an
    /// equivalence class is registered as a key mapped to the other members,
    /// since Meilisearch's synonym table is unidirectional. More groups are
    /// added as the SBL transliteration model is refined with a Syriacist.
    /// </summary>
    private static readonly string[][] SynonymGroups =
    {
        new[] { "meltho", "meltā", "melthā", "meltha" },
    };

    public string IndexName => "lexicon";

    public string PrimaryKey => "id";

    public IndexSettings Settings => new(
        SearchableAttributes: Searchable,
        FilterableAttributes: Filterable,
        Synonyms: BuildEquivalenceClasses(SynonymGroups),
        SortableAttributes: Sortable);

    private static Dictionary<string, IReadOnlyList<string>> BuildEquivalenceClasses(string[][] groups)
    {
        var dict = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            for (var i = 0; i < group.Length; i++)
            {
                var key = group[i];
                dict[key] = group.Where((_, j) => j != i).ToArray();
            }
        }

        return dict;
    }
}
