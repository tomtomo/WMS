using Xunit;

namespace Wms.Outbound.IntegrationTests.TestSupport;

// Seluruh integration test Outbound pakai satu container Postgres.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "outbound-postgres";
}
