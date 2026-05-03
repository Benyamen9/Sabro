namespace Sabro.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class TranslationsCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Translations Postgres";
}
