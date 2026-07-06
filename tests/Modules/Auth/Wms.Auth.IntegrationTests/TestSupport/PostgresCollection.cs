using Xunit;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Seluruh integration test Auth berbagi satu container Postgres.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "auth-postgres";
}
