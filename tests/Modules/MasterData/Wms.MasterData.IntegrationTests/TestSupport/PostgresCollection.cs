using Xunit;

namespace Wms.MasterData.IntegrationTests.TestSupport;

// Seluruh integration test MasterData berbagi satu container Postgres.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "masterdata-postgres";
}
