using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Infrastructure.Eventing;

// Worker untuk menerima event dari rail dan meneruskannya ke consumer modul.
public sealed class RailSubscriberWorker(
    IServiceScopeFactory scopeFactory,
    IMessageSubscriber subscriber,
    IEnumerable<RailConsumerRegistration> registrations,
    EventingRailOptions options,
    ILogger<RailSubscriberWorker> logger) : BackgroundService
{
    private readonly IReadOnlyList<RailConsumerRegistration> _registrations = [.. registrations];

    // Mulai subscription untuk consumer modul.
    internal async Task SubscribeOnceAsync(CancellationToken cancellationToken)
    {
        if (_registrations.Count == 0)
        {
            // Tidak ada consumer yang perlu dijalankan.
            logger.LogInformation("RailSubscriberWorker: modul tak punya consumer; subscriber idle.");
            return;
        }

        var subscriptions = _registrations
            .Select(registration => new RailSubscription(registration.LogicalName, registration.DeliveryClass))
            .Distinct()
            .ToList();

        await subscriber
            .SubscribeAsync(options.ModuleQueueName, subscriptions, DispatchAsync, cancellationToken)
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

    private async Task<bool> DispatchAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        var matches = _registrations
            .Where(registration =>
                registration.DeliveryClass == envelope.DeliveryClass
                && string.Equals(registration.LogicalName, envelope.LogicalName, StringComparison.Ordinal))
            .ToList();

        if (matches.Count == 0)
        {
            // Abaikan message yang tidak punya consumer terdaftar.
            logger.LogWarning(
                "Rail: tak ada consumer untuk {LogicalName}/{DeliveryClass}; ack tanpa proses.",
                envelope.LogicalName,
                envelope.DeliveryClass);
            return true;
        }

        // Jalankan setiap consumer dalam scope terpisah.
        foreach (var registration in matches)
        {
            using var scope = scopeFactory.CreateScope();
            var result = await registration
                .InvokeAsync(scope.ServiceProvider, envelope, cancellationToken)
                .ConfigureAwait(false);
            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Rail: consumer {LogicalName}/{DeliveryClass} gagal ({Error}); nack tanpa requeue.",
                    envelope.LogicalName,
                    envelope.DeliveryClass,
                    result.Error.Code);
                return false;
            }
        }

        return true;
    }
}
