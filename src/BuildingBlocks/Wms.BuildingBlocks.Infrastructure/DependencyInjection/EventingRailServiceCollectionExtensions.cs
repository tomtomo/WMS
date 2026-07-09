using Microsoft.Extensions.DependencyInjection.Extensions;
using Wms.BuildingBlocks.Infrastructure.Eventing;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi worker eventing rail untuk modul.
public static class EventingRailServiceCollectionExtensions
{
    public static IServiceCollection AddEventingRail(this IServiceCollection services, string moduleQueueName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleQueueName);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(new EventingRailOptions { ModuleQueueName = moduleQueueName });

        // Daftarkan worker producer sebagai hosted service.
        services.TryAddSingleton<OutboxDispatcherWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<OutboxDispatcherWorker>());

        // Daftarkan worker consumer sebagai hosted service.
        services.TryAddSingleton<RailSubscriberWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RailSubscriberWorker>());

        return services;
    }

    // Untuk host yang hanya consume event. Daftarkan subscriber rail saja,
    // tanpa outbox dispatcher, karena host ini tidak punya tabel outbox untuk dipolling.
    public static IServiceCollection AddEventingRailSubscriber(this IServiceCollection services, string moduleQueueName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleQueueName);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(new EventingRailOptions { ModuleQueueName = moduleQueueName });

        services.TryAddSingleton<RailSubscriberWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RailSubscriberWorker>());

        return services;
    }
}
