using Testcontainers.EventHubs;
using Xunit;

namespace Wms.Platform.Azure.ParityTests.TestSupport;

// Emulator Event Hubs
public sealed class EventHubsEmulatorFixture : IAsyncLifetime
{
    public const string StreamName = "wms-scan-stream";

    private readonly EventHubsContainer _container =
        new EventHubsBuilder("mcr.microsoft.com/azure-messaging/eventhubs-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .WithConfigurationBuilder(
                EventHubsServiceConfiguration.Create().WithEntity(StreamName, partitionCount: 2))
            .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class EventHubsEmulatorCollection : ICollectionFixture<EventHubsEmulatorFixture>
{
    public const string Name = "eventhubs-emulator";
}
