using Testcontainers.RabbitMq;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

// Satu container RabbitMQ dipakai bersama
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.1-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
