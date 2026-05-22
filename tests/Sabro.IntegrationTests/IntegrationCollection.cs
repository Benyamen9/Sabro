namespace Sabro.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class IntegrationCollection :
    ICollectionFixture<PostgresFixture>,
    ICollectionFixture<MeilisearchFixture>
{
    public const string Name = "Integration Postgres";
}
