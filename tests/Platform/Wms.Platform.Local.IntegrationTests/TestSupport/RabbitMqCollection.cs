using Xunit;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

[CollectionDefinition(Name)]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "rabbitmq";
}
