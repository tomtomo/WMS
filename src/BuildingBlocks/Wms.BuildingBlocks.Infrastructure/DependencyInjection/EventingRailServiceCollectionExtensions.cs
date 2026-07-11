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
        services.TryAddSingleton<RailConsumerDispatcher>();

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
        services.TryAddSingleton<RailConsumerDispatcher>();

        services.TryAddSingleton<RailSubscriberWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RailSubscriberWorker>());

        return services;
    }

    // Untuk host serverless yang menerima message dari trigger platform seperti Functions atau Cloud Run:
    // hanya dispatcher yang dipasang, karena proses subscribe dan siklus hidup message ditangani oleh trigger, bukan worker.
    public static IServiceCollection AddEventingRailDispatchOnly(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<RailConsumerDispatcher>();

        return services;
    }
}
