using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;

// Seluruh integration test berbagi satu container
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
