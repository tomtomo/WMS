using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Subscriptions;

// Menentukan target notifikasi dari subscription yang tersedia.
public sealed class SubscriptionResolver(INotificationSubscriptionReader reader, IUserDirectory directory)
{
    public async Task<IReadOnlyList<ResolvedTarget>> ResolveAsync(
        string eventType,
        Guid? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var matches = await reader.ListForEventAsync(eventType, cancellationToken);

        var candidates = new List<ResolvedTarget>();
        foreach (var match in matches.Where(match => ScopeMatches(match.WarehouseScope, warehouseId)))
        {
            IReadOnlyList<Guid> userIds = match.SubscriberType == SubscriberType.User
                ? [match.SubscriberId]
                : await directory.GetUsersInRoleAsync(match.SubscriberId, cancellationToken);

            candidates.AddRange(
                from userId in userIds
                from channel in match.Channels
                select new ResolvedTarget(userId, channel, match.SubscriptionId));
        }

        // Hindari notifikasi ganda untuk user dan channel yang sama.
        return candidates
            .GroupBy(target => (target.UserId, target.Channel))
            .Select(group => group.First())
            .ToList();
    }

    // Subscription tanpa warehouse scope berlaku untuk semua warehouse.
    private static bool ScopeMatches(Guid? warehouseScope, Guid? warehouseId) =>
        warehouseScope is null || warehouseScope == warehouseId;
}
