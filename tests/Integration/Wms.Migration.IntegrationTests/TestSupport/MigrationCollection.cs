using Xunit;

namespace Wms.Migration.IntegrationTests.TestSupport;

[CollectionDefinition(Name)]
public sealed class MigrationCollection : ICollectionFixture<MigrationFixture>
{
    public const string Name = "migration";
}
