using Xunit;

namespace Wms.Inventory.IntegrationTests.TestSupport;

// Seluruh integration test Inventory pakai satu container Postgres.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "inventory-postgres";
}
