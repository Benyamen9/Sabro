namespace Sabro.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class TranslationsCollection :
    ICollectionFixture<PostgresFixture>,
    ICollectionFixture<MeilisearchFixture>
{
    public const string Name = "Translations Postgres";
}
