using Azure;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.BuildingBlocks.Infrastructure.Telemetry;
using Wms.Platform.Azure.Eventing;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.Saga;
using Wms.Platform.Azure.Scheduling;

namespace Microsoft.Extensions.DependencyInjection;

// Kumpulan registrasi service untuk mode Azure.
public static class AzurePlatformServiceCollectionExtensions
{
    public static IServiceCollection AddAzurePlatform(this IServiceCollection services, IConfiguration configuration) =>
        services.AddAzureMessaging(configuration);

    public static IServiceCollection AddAzureMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(configuration);
        services.TryAddSingleton(TimeProvider.System);
        services.AddLogging();

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

        // DurableTaskClient disediakan oleh host worker. Kalau belum tersedia, resolve dependency akan gagal lebih awal.
        services.TryAddSingleton<ISagaOrchestrator, DurableFunctionsSagaOrchestrator>();

        services.TryAddSingleton<IDelayedTaskQueue, ServiceBusScheduledDelayedTaskQueue>();
        services.TryAddSingleton<IRecurringJobScheduler, FunctionsTimerRecurringJobScheduler>();

        services.TryAddSingleton<ITelemetrySink, TelemetrySink>();

        return services;
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
