using System.Diagnostics;
using Azure;
using Azure.Communication.Email;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Platform.Azure.Cache;
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

namespace Microsoft.Extensions.DependencyInjection;

// Kumpulan registrasi service untuk mode Azure.
public static class AzurePlatformServiceCollectionExtensions
{
    public static IServiceCollection AddAzurePlatform(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddAzureMessaging(configuration)
            .AddAzureSecurity(configuration)
            .AddAzurePersistence(configuration)
            .AddAzureTelemetry(configuration)
            .AddAzureNotifications(configuration);

    public static IServiceCollection AddAzureMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        AddAzureCore(services, configuration);

        services.AddValidatedOptions<AzureMessagingOptions>(AzureMessagingOptions.SectionName);

        // Client SDK dibuat singleton karena aman dipakai ulang.
        services.TryAddSingleton(CreateServiceBusClient);
        services.TryAddSingleton(CreateServiceBusAdministrationClient);
        services.TryAddSingleton(CreateEventGridPublisherClient);

        // Dispatcher tetap satu, tapi transport dipilih berdasarkan delivery class.
        services.TryAddSingleton<ServiceBusMessagePublisher>();
        services.TryAddSingleton<EventGridNotificationPublisher>();
        services.TryAddSingleton<OutboxDispatcher, AzureOutboxDispatcher>();
        services.TryAddSingleton<IMessageSubscriber, ServiceBusMessageSubscriber>();
        services.TryAddSingleton<ServiceBusDeadLetterStore>();

        // Bagian rail yang tetap disimpan di database modul: outbox, inbox , dead letter dan audit.
        services.TryAddScoped<IIntegrationEventOutbox, IntegrationEventOutbox>();
        services.TryAddScoped<IInboxGuard, InboxGuard>();
        services.TryAddScoped<IDeadLetterStore, DeadLetterStore>();
        services.TryAddSingleton<IAuditLogStore, AuditLogStore>();

        services.TryAddSingleton<IEventStreamPublisher, EventHubsEventStreamPublisher>();

        // DurableTaskClient disediakan oleh worker host dan harus tersedia saat ISagaOrchestrator dibuat.
        services.TryAddSingleton<ISagaOrchestrator, DurableFunctionsSagaOrchestrator>();

        services.TryAddSingleton<IDelayedTaskQueue, ServiceBusScheduledDelayedTaskQueue>();
        services.TryAddSingleton<IRecurringJobScheduler, FunctionsTimerRecurringJobScheduler>();

        return services;
    }

    // Security didaftarkan lebih dulu karena Flexible Server mengambil passwordnya dari Key Vault saat startup.
    public static IServiceCollection AddAzureSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        AddAzureCore(services, configuration);

        services.AddValidatedOptions<KeyVaultOptions>(KeyVaultOptions.SectionName);

        services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.TryAddSingleton(CreateSecretClient);
        services.TryAddSingleton<ISecretProvider, KeyVaultSecretProvider>();
        services.TryAddSingleton<IServiceTokenProvider, ManagedIdentityTokenProvider>();

        // Argon2id memakai adapter yang sama dengan Local, bukan implementasi ulang.
        services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

        return services;
    }

    public static IServiceCollection AddAzurePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        AddAzureCore(services, configuration);

        services.AddValidatedOptions<FlexibleServerOptions>(FlexibleServerOptions.SectionName);
        services.AddValidatedOptions<CosmosOptions>(CosmosOptions.SectionName);
        services.AddValidatedOptions<BlobObjectStoreOptions>(BlobObjectStoreOptions.SectionName);
        services.AddValidatedOptions<AzureCacheOptions>(AzureCacheOptions.SectionName);

        // Command side tetap memakai EF dan Npgsql, yang disediakan di sini hanya connection string untuk Flexible Server.
        services.TryAddSingleton<FlexibleServerConnectionStringFactory>();

        services.TryAddSingleton(CreateCosmosClient);
        services.TryAddSingleton<IProjectionStore, CosmosProjectionStore>();
        services.TryAddSingleton<IProjectionChangeHandler, CacheInvalidatingProjectionChangeHandler>();
        services.AddHostedService<CosmosChangeFeedProcessor>();

        services.TryAddSingleton(CreateBlobServiceClient);
        services.TryAddSingleton<IObjectStore, BlobObjectStore>();

        // Adapter Redis ada di Platform.Shared: Managed Redis dan Memorystore menggunakan protokol yang sama.
        services.TryAddSingleton(CreateRedisMultiplexer);
        services.TryAddSingleton<ICacheStore, RedisCacheStore>();
        services.TryAddSingleton<IApiIdempotencyStore, RedisApiIdempotencyStore>();

        return services;
    }

    public static IServiceCollection AddAzureTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        AddAzureCore(services, configuration);

        services.AddValidatedOptions<AppInsightsOptions>(AppInsightsOptions.SectionName);
        services.TryAddSingleton(provider => new ActivitySource(
            provider.GetRequiredService<IOptions<AppInsightsOptions>>().Value.ServiceName));
        services.TryAddSingleton<ITelemetrySink, AppInsightsTelemetrySink>();

        var telemetryOptions = new AppInsightsOptions();
        configuration.GetSection(AppInsightsOptions.SectionName).Bind(telemetryOptions);
        var connectionString = configuration.GetConnectionString(telemetryOptions.ConnectionStringName);

        // Kalau belum ada connection string, telemetry dilewati supaya app tetap bisa start.
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    .AddSource(telemetryOptions.ServiceName)
                    .AddAzureMonitorTraceExporter(exporter => exporter.ConnectionString = connectionString))
                .WithMetrics(metrics => metrics
                    .AddAzureMonitorMetricExporter(exporter => exporter.ConnectionString = connectionString));
        }

        return services;
    }

    public static IServiceCollection AddAzureNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        AddAzureCore(services, configuration);

        services.AddValidatedOptions<AcsEmailOptions>(AcsEmailOptions.SectionName);
        services.TryAddSingleton(CreateEmailClient);
        services.TryAddSingleton<IEmailSender, AcsEmailSender>();

        // Firebase dan SignalR disiapkan oleh host, di sini cukup mengambil dependency yang sudah terpasang.
        services.TryAddSingleton<IFirebaseMessagingClient>(provider =>
            new FirebaseAdminMessagingClient(FirebaseMessaging.GetMessaging(provider.GetRequiredService<FirebaseApp>())));
        services.TryAddSingleton<IPushNotifier, FcmPushNotifier>();
        services.TryAddSingleton<IInAppNotifier, SignalRInAppNotifier>();

        return services;
    }

    private static void AddAzureCore(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(configuration);
        services.TryAddSingleton(TimeProvider.System);
        services.AddLogging();
    }

    private static ServiceBusClient CreateServiceBusClient(IServiceProvider provider) =>
        new(ResolveServiceBusConnectionString(provider));

    private static ServiceBusAdministrationClient CreateServiceBusAdministrationClient(IServiceProvider provider) =>
        new(ResolveServiceBusConnectionString(provider));

    private static EventGridPublisherClient CreateEventGridPublisherClient(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<AzureMessagingOptions>>().Value;
        return new EventGridPublisherClient(
            new Uri(options.EventGridTopicEndpoint),
            new AzureKeyCredential(options.EventGridTopicKey));
    }

    private static SecretClient CreateSecretClient(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<KeyVaultOptions>>().Value;
        return new SecretClient(options.VaultUri!, provider.GetRequiredService<TokenCredential>());
    }

    // Gunakan Managed Identity di production, dan connection string untuk emulator atau test.
    private static CosmosClient CreateCosmosClient(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<CosmosOptions>>().Value;
        if (options.AccountEndpoint is not null)
        {
            return CosmosClientFactory.CreateWithManagedIdentity(
                options.AccountEndpoint,
                provider.GetRequiredService<TokenCredential>(),
                options);
        }

        var configuration = provider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                $"Cosmos butuh 'AccountEndpoint' atau connection string '{options.ConnectionStringName}'.")
            : CosmosClientFactory.CreateWithConnectionString(connectionString, options);
    }

    private static BlobServiceClient CreateBlobServiceClient(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<BlobObjectStoreOptions>>().Value;

        // Gunakan kredensial Azure agar SAS dapat dibuat tanpa account key.
        return new BlobServiceClient(options.AccountUrl!, provider.GetRequiredService<TokenCredential>());
    }

    private static IConnectionMultiplexer CreateRedisMultiplexer(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<AzureCacheOptions>>().Value;
        var configuration = provider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{options.ConnectionStringName}' untuk Managed Redis tidak ditemukan di konfigurasi.");

        var redisOptions = ConfigurationOptions.Parse(connectionString);

        // Tetap jalankan aplikasi saat Redis belum dapat terhubung.
        redisOptions.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(redisOptions);
    }

    private static EmailClient CreateEmailClient(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<AcsEmailOptions>>().Value;
        if (options.Endpoint is not null)
        {
            return new EmailClient(options.Endpoint, provider.GetRequiredService<TokenCredential>());
        }

        var configuration = provider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                $"ACS butuh 'Endpoint' atau connection string '{options.ConnectionStringName}'.")
            : new EmailClient(connectionString);
    }

    private static string ResolveServiceBusConnectionString(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<AzureMessagingOptions>>().Value;
        var configuration = provider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString(options.ServiceBusConnectionStringName);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                $"Connection string '{options.ServiceBusConnectionStringName}' untuk Service Bus tidak ditemukan di konfigurasi.")
            : connectionString;
    }
}
