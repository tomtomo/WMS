using Xunit;

namespace Wms.Inbound.IntegrationTests.TestSupport;

// Seluruh integration test Inbound pakai satu container Postgres.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "inbound-postgres";
}
