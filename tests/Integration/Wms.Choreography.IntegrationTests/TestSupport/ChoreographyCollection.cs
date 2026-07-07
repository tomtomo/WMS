using Xunit;

namespace Wms.Choreography.IntegrationTests.TestSupport;

[CollectionDefinition(Name)]
public sealed class ChoreographyCollection : ICollectionFixture<ChoreographyFixture>
{
    public const string Name = "choreography";
}
