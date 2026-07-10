using Testcontainers.ServiceBus;
using Xunit;

namespace Wms.Platform.Azure.ParityTests.TestSupport;

// Satu emulator Service Bus dipakai seluruh test collection, offline, tanpa cloud.
public sealed class ServiceBusEmulatorFixture : IAsyncLifetime
{
    private static readonly string? _liveConnectionString = Environment.GetEnvironmentVariable("WMS_PARITY_SB_CONN");

    private readonly ServiceBusContainer? _container = _liveConnectionString is null
        ? new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .Build()
        : null;

    public string ConnectionString => _liveConnectionString ?? _container!.GetConnectionString();

    // Management REST emulator ada di port 5300, terpisah dari AMQP 5672. Jika di Azure cloud satu endpoint untuk keduanya.
    public string AdministrationConnectionString =>
        _liveConnectionString
        ?? $"Endpoint=sb://{_container!.Hostname}:{_container.GetMappedPublicPort(ServiceBusBuilder.ServiceBusHttpPort)};" +
           "SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

    public Task InitializeAsync() => _container?.StartAsync() ?? Task.CompletedTask;

    public Task DisposeAsync() => _container?.DisposeAsync().AsTask() ?? Task.CompletedTask;
}

[CollectionDefinition(Name)]
public sealed class ServiceBusEmulatorCollection : ICollectionFixture<ServiceBusEmulatorFixture>
{
    public const string Name = "servicebus-emulator";
}
