using Microsoft.Extensions.DependencyInjection;
using Wms.Notifications.Abstractions;

namespace Wms.Notifications.Deliveries;

// Memproses delivery pending dalam scope terpisah
internal sealed class DeliveryDispatchRunner(IServiceScopeFactory scopeFactory)
{
    public const int BatchSize = 100;

    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DeliveryId> pendingIds;
        using (var scope = scopeFactory.CreateScope())
        {
            pendingIds = await scope.ServiceProvider
                .GetRequiredService<INotificationDeliveryRepository>()
                .ListPendingIdsAsync(BatchSize, cancellationToken);
        }

        var dispatched = 0;
        foreach (var id in pendingIds)
        {
            using var scope = scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<DeliveryDispatcher>();
            if (await dispatcher.TryDispatchAsync(id, cancellationToken))
            {
                dispatched++;
            }
        }

        return dispatched;
    }
}
