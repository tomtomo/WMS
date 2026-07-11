using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Infrastructure.Eventing;

// Worker untuk menerima event dari rail dan meneruskannya ke consumer modul.
public sealed class RailSubscriberWorker(
    IMessageSubscriber subscriber,
    RailConsumerDispatcher dispatcher,
    EventingRailOptions options,
    ILogger<RailSubscriberWorker> logger) : BackgroundService
{
    // Mulai subscription untuk consumer modul.
    internal async Task SubscribeOnceAsync(CancellationToken cancellationToken)
    {
        if (dispatcher.Registrations.Count == 0)
        {
            // Tidak ada consumer yang perlu dijalankan.
            logger.LogInformation("RailSubscriberWorker: modul tak punya consumer; subscriber idle.");
            return;
        }

        var subscriptions = dispatcher.Registrations
            .Select(registration => new RailSubscription(registration.LogicalName, registration.DeliveryClass))
            .Distinct()
            .ToList();

        await subscriber
            .SubscribeAsync(options.ModuleQueueName, subscriptions, dispatcher.DispatchAsync, cancellationToken)
            .ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SubscribeOnceAsync(stoppingToken).ConfigureAwait(false);

        // Pertahankan worker tetap aktif sampai host dihentikan.
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown normal.
        }
    }
}
