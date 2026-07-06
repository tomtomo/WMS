using Xunit;

namespace Wms.Reporting.IntegrationTests.TestSupport;

// Seluruh integration test Reporting berbagi satu container Postgres.
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "reporting-postgres";
}
