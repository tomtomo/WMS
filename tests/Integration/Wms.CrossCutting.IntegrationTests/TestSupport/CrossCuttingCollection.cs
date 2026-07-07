using Xunit;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

[CollectionDefinition(Name)]
public sealed class CrossCuttingCollection : ICollectionFixture<CrossCuttingFixture>
{
    public const string Name = "cross-cutting";
}
