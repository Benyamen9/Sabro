using Sabro.Shared.Infrastructure.Search;
using Sabro.Shared.Search;

namespace Sabro.UnitTests.Shared.Search;

public class IndexSettingsMappingTests
{
    private static readonly string[] LexiconSearchableAttributes = { "syriacUnvocalized", "sblTransliteration" };

    private static readonly string[] MelthoVariants = { "meltā", "melthā", "meltha" };

    [Fact]
    public void ToMeilisearchSettings_WithEmptyInput_LeavesAllFieldsNull()
    {
        var meili = SearchIndexInitializerHostedService.ToMeilisearchSettings(IndexSettings.Empty);

        meili.SearchableAttributes.Should().BeNull();
        meili.FilterableAttributes.Should().BeNull();
        meili.Synonyms.Should().BeNull();
    }

    [Fact]
    public void ToMeilisearchSettings_PassesSearchableAttributesThrough()
    {
        var source = new IndexSettings(
            SearchableAttributes: LexiconSearchableAttributes,
            FilterableAttributes: Array.Empty<string>(),
            Synonyms: new Dictionary<string, IReadOnlyList<string>>());

        var meili = SearchIndexInitializerHostedService.ToMeilisearchSettings(source);

        meili.SearchableAttributes.Should().BeEquivalentTo(
            LexiconSearchableAttributes,
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void ToMeilisearchSettings_FlattensSynonymGroups()
    {
        var source = new IndexSettings(
            SearchableAttributes: Array.Empty<string>(),
            FilterableAttributes: Array.Empty<string>(),
            Synonyms: new Dictionary<string, IReadOnlyList<string>>
            {
                ["meltho"] = MelthoVariants,
            });

        var meili = SearchIndexInitializerHostedService.ToMeilisearchSettings(source);

        meili.Synonyms.Should().NotBeNull();
        meili.Synonyms!["meltho"].Should().BeEquivalentTo(MelthoVariants);
    }
}
