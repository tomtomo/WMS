using AwesomeAssertions;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Platform.Azure.Eventing;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.Notifications;
using Wms.Platform.Azure.ObjectStore;
using Wms.Platform.Azure.Persistence;
using Wms.Platform.Azure.Saga;
using Wms.Platform.Azure.Scheduling;
using Wms.Platform.Azure.Secrets;
using Wms.Platform.Azure.Security;
using Wms.Platform.Azure.Telemetry;
using Wms.Platform.Shared.Cache;
using Wms.Platform.Shared.Notifications;
using Wms.Platform.Shared.Security;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Composition root harus memasang adapter Azure di balik port yang sama.
public sealed class AzureCompositionTests
{
    [Fact]
    public async Task Add_azure_platform_binds_every_messaging_port_to_its_azure_adapter()
    {
        // Sender adapter memakai IAsyncDisposable, jadi provider harus ditutup lewat DisposeAsync.
        await using var provider = BuildProvider();

        provider.GetRequiredService<OutboxDispatcher>().Should().BeOfType<AzureOutboxDispatcher>();
        provider.GetRequiredService<IMessageSubscriber>().Should().BeOfType<ServiceBusMessageSubscriber>();
        provider.GetRequiredService<IEventStreamPublisher>().Should().BeOfType<EventHubsEventStreamPublisher>();
        provider.GetRequiredService<ISagaOrchestrator>().Should().BeOfType<DurableFunctionsSagaOrchestrator>();
        provider.GetRequiredService<IDelayedTaskQueue>().Should().BeOfType<ServiceBusScheduledDelayedTaskQueue>();
        provider.GetRequiredService<IRecurringJobScheduler>().Should().BeOfType<FunctionsTimerRecurringJobScheduler>();
        provider.GetRequiredService<ServiceBusDeadLetterStore>().Should().NotBeNull();
    }

    [Fact]
    public async Task Add_azure_platform_binds_every_persistence_port_to_its_azure_adapter()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<IProjectionStore>().Should().BeOfType<CosmosProjectionStore>();
        provider.GetRequiredService<IObjectStore>().Should().BeOfType<BlobObjectStore>();
        provider.GetRequiredService<IProjectionChangeHandler>().Should().BeOfType<CacheInvalidatingProjectionChangeHandler>();
        provider.GetRequiredService<FlexibleServerConnectionStringFactory>().Should().NotBeNull();

        // Pastikan change feed dijalankan sebagai hosted service.
        provider.GetServices<IHostedService>().Should().ContainSingle(service => service is CosmosChangeFeedProcessor);
    }

    [Fact]
    public async Task Add_azure_platform_reuses_the_portable_adapters_shared_with_gcp()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<ICacheStore>().Should().BeOfType<RedisCacheStore>();
        provider.GetRequiredService<IApiIdempotencyStore>().Should().BeOfType<RedisApiIdempotencyStore>();
        provider.GetRequiredService<IPasswordHasher>().Should().BeOfType<Argon2idPasswordHasher>();

        // Pastikan adapter yang digunakan berasal dari project shared.
        typeof(RedisCacheStore).Assembly.GetName().Name.Should().Be("Wms.Platform.Shared");
        typeof(Argon2idPasswordHasher).Assembly.GetName().Name.Should().Be("Wms.Platform.Shared");
        typeof(FcmPushNotifier).Assembly.GetName().Name.Should().Be("Wms.Platform.Shared");
        typeof(SignalRInAppNotifier).Assembly.GetName().Name.Should().Be("Wms.Platform.Shared");
    }

    [Fact]
    public async Task Add_azure_platform_binds_every_security_and_telemetry_port_to_its_azure_adapter()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<ISecretProvider>().Should().BeOfType<KeyVaultSecretProvider>();
        provider.GetRequiredService<IServiceTokenProvider>().Should().BeOfType<ManagedIdentityTokenProvider>();
        provider.GetRequiredService<ITelemetrySink>().Should().BeOfType<AppInsightsTelemetrySink>();
    }

    [Fact]
    public async Task Add_azure_platform_binds_every_notification_channel_to_its_provider()
    {
        await using var provider = BuildProvider();
        provider.GetRequiredService<IEmailSender>().Should().BeOfType<AcsEmailSender>();

        // FirebaseApp dan IHubContext dipasang host, jadi yang bisa diperiksa di sini adalah registrasinya.
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration());
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPushNotifier) && descriptor.ImplementationType == typeof(FcmPushNotifier));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IInAppNotifier) && descriptor.ImplementationType == typeof(SignalRInAppNotifier));
    }

    [Fact]
    public void Rail_invariant_services_stay_registered_for_the_module_db()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration());

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIntegrationEventOutbox));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IInboxGuard));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDeadLetterStore));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAuditLogStore));
    }

    [Fact]
    public async Task Missing_event_grid_configuration_fails_fast_on_options_resolve()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration(includeEventGrid: false));
        await using var provider = services.BuildServiceProvider();

        var resolve = () => provider.GetRequiredService<IOptions<AzureMessagingOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>("endpoint/key Event Grid wajib dikonfigurasi");
    }

    [Fact]
    public async Task Missing_key_vault_configuration_fails_fast_on_options_resolve()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration(includeKeyVault: false));
        await using var provider = services.BuildServiceProvider();

        var resolve = () => provider.GetRequiredService<IOptions<KeyVaultOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>("URI Key Vault wajib dikonfigurasi");
    }

    [Fact]
    public async Task Missing_object_store_configuration_fails_fast_on_options_resolve()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration(includeObjectStore: false));
        await using var provider = services.BuildServiceProvider();

        var resolve = () => provider.GetRequiredService<IOptions<BlobObjectStoreOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>("URL storage account wajib dikonfigurasi");
    }

    private static ServiceProvider BuildProvider()
    {
        var services = NewServices();
        services.AddAzurePlatform(NewConfiguration());
        return services.BuildServiceProvider();
    }

    private static ServiceCollection NewServices()
    {
        var services = new ServiceCollection();

        // DurableTaskClient milik host worker.
        services.AddSingleton(Substitute.For<DurableTaskClient>("composition-test"));
        return services;
    }

    private static IConfiguration NewConfiguration(
        bool includeEventGrid = true,
        bool includeKeyVault = true,
        bool includeObjectStore = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:servicebus"] =
                "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
            ["ConnectionStrings:eventhubs"] =
                "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",

            // Endpoint emulator Cosmos dan Redis: komposisi tidak membuka koneksi apa pun.
            ["ConnectionStrings:cosmos"] =
                "AccountEndpoint=https://localhost:8081/;AccountKey=Q29tcG9zaXRpb25UZXN0S2V5Rm9yQ29zbW9zRW11bGF0b3I=;",
            ["ConnectionStrings:redis"] = "localhost:6379",
            ["ConnectionStrings:acs"] = "endpoint=https://wms.communication.azure.com/;accesskey=Y29tcG9zaXRpb24=",
            ["AzurePlatform:Notifications:Acs:SenderAddress"] = "DoNotReply@wms.example.net",
        };

        if (includeEventGrid)
        {
            values["AzurePlatform:Messaging:EventGridTopicEndpoint"] = "https://wms-notif.westeurope-1.eventgrid.azure.net/api/events";
            values["AzurePlatform:Messaging:EventGridTopicKey"] = "parity-test-key";
        }

        if (includeKeyVault)
        {
            values["AzurePlatform:Secrets:VaultUri"] = "https://wms-vault.vault.azure.net/";
        }

        if (includeObjectStore)
        {
            values["AzurePlatform:ObjectStore:AccountUrl"] = "https://wmsattachments.blob.core.windows.net";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
