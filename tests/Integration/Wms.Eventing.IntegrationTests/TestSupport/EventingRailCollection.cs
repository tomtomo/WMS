using Xunit;

namespace Wms.Eventing.IntegrationTests.TestSupport;

[CollectionDefinition(Name)]
public sealed class EventingRailCollection : ICollectionFixture<EventingRailFixture>
{
    public const string Name = "eventing-rail";
}
