using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Platform.Azure.Eventing;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.Notifications;
using Wms.Platform.Azure.ObjectStore;
using Wms.Platform.Azure.Persistence;
using Wms.Platform.Azure.Scheduling;
using Wms.Platform.Azure.Secrets;
using Wms.Platform.Azure.Security;
using Wms.Platform.Azure.Telemetry;
using Wms.Platform.Shared.Cache;
using Wms.Platform.Shared.Notifications;
using Wms.Platform.Shared.Security;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Pastikan adapter Azure terpasang pada port yang sama dengan urutan registrasi seperti di host aslinya.
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
        provider.GetRequiredService<IDelayedTaskQueue>().Should().BeOfType<ServiceBusScheduledDelayedTaskQueue>();
        provider.GetRequiredService<IRecurringJobScheduler>().Should().BeOfType<FunctionsTimerRecurringJobScheduler>();
    }

    [Fact]
    public async Task Add_azure_platform_binds_object_store_to_blob_adapter()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<IObjectStore>().Should().BeOfType<BlobObjectStore>();
    }

    [Fact]
    public async Task Add_azure_platform_binds_operational_telemetry_store_to_cosmos_adapter()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<IOperationalTelemetryStore>().Should().BeOfType<CosmosOperationalTelemetryStore>();
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
    public async Task Add_azure_platform_binds_security_ports_to_azure_adapters()
    {
        await using var provider = BuildProvider();

        provider.GetRequiredService<ISecretProvider>().Should().BeOfType<KeyVaultSecretProvider>();
        provider.GetRequiredService<IServiceTokenProvider>().Should().BeOfType<ManagedIdentityTokenProvider>();
    }

    [Fact]
    public async Task Telemetry_sink_replaces_the_infrastructure_default_in_production_order()
    {
        // sink default Infrastructure terdaftar lebih dulu
        await using var provider = BuildProvider();

        provider.GetRequiredService<ITelemetrySink>().Should().BeOfType<AppInsightsTelemetrySink>();
        provider.GetRequiredService<IAnalyticsSink>().Should().BeOfType<AppInsightsAnalyticsSink>();
    }

    [Fact]
    public async Task Add_azure_notifications_is_explicit_and_binds_every_channel()
    {
        // Notifier dipasang oleh host pengirim notifikasi.
        var configuration = NewConfiguration();
        var services = NewProductionOrderServices(configuration);
        services.AddAzureNotifications(configuration);
        await using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IEmailSender>().Should().BeOfType<AcsEmailSender>();

        // FirebaseApp dan IHubContext dipasang host, jadi yang bisa diperiksa di sini adalah registrasinya.
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPushNotifier) && descriptor.ImplementationType == typeof(FcmPushNotifier));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IInAppNotifier) && descriptor.ImplementationType == typeof(SignalRInAppNotifier));
    }

    [Fact]
    public void Add_azure_platform_does_not_register_notifiers()
    {
        var services = new ServiceCollection();
        services.AddAzurePlatform(NewConfiguration());

        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IPushNotifier));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IInAppNotifier));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IEmailSender));
    }

    [Fact]
    public void Rail_invariant_services_stay_registered_for_the_module_db()
    {
        // Registrasi utama berasal dari AddBuildingBlocksInfrastructure, jadi platform tidak menambahkan ulang dependency yang sama.
        var configuration = NewConfiguration();
        var services = NewProductionOrderServices(configuration);
        services.AddAzurePlatform(configuration);

        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IIntegrationEventOutbox));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IInboxGuard));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDeadLetterStore));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAuditLogStore));
    }

    [Fact]
    public async Task Missing_event_grid_configuration_fails_fast_on_options_resolve()
    {
        var configuration = NewConfiguration(includeEventGrid: false);
        var services = NewProductionOrderServices(configuration);
        services.AddAzurePlatform(configuration);
        await using var provider = services.BuildServiceProvider();

        var resolve = () => provider.GetRequiredService<IOptions<AzureMessagingOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>("endpoint/key Event Grid wajib dikonfigurasi");
    }

    [Fact]
    public async Task Missing_key_vault_configuration_fails_fast_on_options_resolve()
    {
        var configuration = NewConfiguration(includeKeyVault: false);
        var services = NewProductionOrderServices(configuration);
        services.AddAzurePlatform(configuration);
        await using var provider = services.BuildServiceProvider();

        var resolve = () => provider.GetRequiredService<IOptions<KeyVaultOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>("URI Key Vault wajib dikonfigurasi");
    }

    [Fact]
    public async Task Missing_object_store_configuration_fails_fast_on_options_resolve()
    {
        var configuration = NewConfiguration(includeObjectStore: false);
        var services = NewProductionOrderServices(configuration);
        services.AddAzurePlatform(configuration);
        await using var provider = services.BuildServiceProvider();

        var resolve = () => provider.GetRequiredService<IOptions<BlobObjectStoreOptions>>().Value;

        resolve.Should().Throw<OptionsValidationException>("URL storage account wajib dikonfigurasi");
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = NewConfiguration();
        var services = NewProductionOrderServices(configuration);
        services.AddAzurePlatform(configuration);
        return services.BuildServiceProvider();
    }

    // Ikuti urutan registrasi di host Azure: pasang IConfiguration lebih dulu, lalu infrastructure, dan platform setelahnya.
    // Jika IConfiguration belum tersedia, AddOpenTelemetry dapat mendaftarkan konfigurasi kosong.
    private static ServiceCollection NewProductionOrderServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddBuildingBlocksInfrastructure("wms-parity");
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
            ["ConnectionStrings:cosmos"] =
                "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;",
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
