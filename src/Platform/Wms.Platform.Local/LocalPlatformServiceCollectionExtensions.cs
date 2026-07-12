using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.BuildingBlocks.Infrastructure.Telemetry;
using Wms.Platform.Local.Analytics;
using Wms.Platform.Local.Cache;
using Wms.Platform.Local.Eventing;
using Wms.Platform.Local.Messaging;
using Wms.Platform.Local.Notifications;
using Wms.Platform.Local.ObjectStore;
using Wms.Platform.Local.Persistence;
using Wms.Platform.Local.Saga;
using Wms.Platform.Local.Scheduling;
using Wms.Platform.Local.Secrets;
using Wms.Platform.Local.Security;
using Wms.Platform.Local.Streaming;
using Wms.Platform.Shared.Security;

namespace Microsoft.Extensions.DependencyInjection;

// Composition entry point mode Local.
public static class LocalPlatformServiceCollectionExtensions
{
    public static IServiceCollection AddLocalPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(configuration);
        services.TryAddSingleton(TimeProvider.System);
        services.AddLogging();

        services.AddValidatedOptions<RabbitMqOptions>(RabbitMqOptions.SectionName);
        services.AddValidatedOptions<LocalDatabaseOptions>(LocalDatabaseOptions.SectionName);
        services.AddValidatedOptions<FileSystemObjectStoreOptions>(FileSystemObjectStoreOptions.SectionName);
        services.AddValidatedOptions<EnvSecretOptions>(EnvSecretOptions.SectionName);

        // Registrasi messaging RabbitMQ.
        services.TryAddSingleton<RabbitMqConnectionFactory>();
        services.TryAddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
        services.TryAddSingleton<IMessageSubscriber, RabbitMqMessageSubscriber>();

        // Gunakan RabbitMQ untuk dispatch outbox. Outbox/inbox/dead-letter/audit rail tidak
        // didaftarkan di sini.
        services.TryAddSingleton<OutboxDispatcher, RabbitMqOutboxDispatcher>();

        // Store Local NpgsqlDataSource (cloud: Redis). Read model = Postgres per modul
        services.TryAddSingleton(CreateDataSource);
        services.TryAddSingleton<IApiIdempotencyStore, PostgresApiIdempotencyStore>();

        services.TryAddSingleton<ICacheStore, InMemoryCacheStore>();

        // Gunakan object store lokal berbasis file system.
        services.TryAddSingleton<FileSystemObjectStore>();
        services.TryAddSingleton<IObjectStore>(provider => provider.GetRequiredService<FileSystemObjectStore>());

        services.TryAddSingleton<ISecretProvider, EnvSecretProvider>();
        services.TryAddSingleton<IPasswordHasher, Argon2idPasswordHasher>();
        services.TryAddSingleton<IServiceTokenProvider, TrustStubServiceTokenProvider>();

        AddHangfireScheduling(services);

        services.TryAddSingleton<IEmailSender, LoggingEmailSender>();
        services.TryAddSingleton<IPushNotifier, LoggingPushNotifier>();
        services.TryAddSingleton<IInAppNotifier, LoggingInAppNotifier>();

        services.TryAddSingleton<ITelemetrySink, TelemetrySink>();

        // Gunakan singleton karena status saga disimpan di memory dan harus memakai instance yang sama.
        // Modul tetap dapat menggantinya dengan implement sendiri sebelum registrasi ini dijalankan.
        services.TryAddSingleton<ISagaOrchestrator, InProcSagaOrchestrator>();

        services.TryAddSingleton<InProcStreamRing>();
        services.TryAddSingleton<IEventStreamPublisher, InProcStreamPublisher>();
        services.TryAddSingleton<IEventStreamConsumer, InProcStreamConsumer>();
        services.TryAddSingleton<IAnalyticsSink, LogCsvAnalyticsSink>();

        return services;
    }

    private static void AddHangfireScheduling(IServiceCollection services)
    {
        // JobStorage = penanda AddHangfire sudah jalan (host boleh memanggil AddLocalPlatform setelah AddHangfire).
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(JobStorage)))
        {
            services.AddHangfire((provider, config) => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(bootstrap =>
                    bootstrap.UseNpgsqlConnection(ResolveDatabaseConnectionString(provider))));
        }

        services.TryAddSingleton<IRecurringJobScheduler, HangfireRecurringJobScheduler>();
        services.TryAddSingleton<IDelayedTaskQueue, HangfireDelayedTaskQueue>();
    }

    private static NpgsqlDataSource CreateDataSource(IServiceProvider provider) =>
        NpgsqlDataSource.Create(ResolveDatabaseConnectionString(provider));

    private static string ResolveDatabaseConnectionString(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<LocalDatabaseOptions>>().Value;
        var configuration = provider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString(options.ConnectionStringName);

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                $"Connection string '{options.ConnectionStringName}' untuk Postgres Local tidak ditemukan di konfigurasi.")
            : connectionString;
    }
}
