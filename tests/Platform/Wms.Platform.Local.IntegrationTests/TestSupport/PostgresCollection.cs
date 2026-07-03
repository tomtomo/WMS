using Xunit;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

// Test berbasis Postgres berbagi satu container (serial dalam collection).
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
