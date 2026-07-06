using Xunit;

namespace Wms.Notifications.IntegrationTests.TestSupport;

// Seluruh integration test Notifications berbagi satu container Postgres.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "notifications-postgres";
}
