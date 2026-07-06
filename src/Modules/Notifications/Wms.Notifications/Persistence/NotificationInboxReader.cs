using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries;
using Wms.Notifications.ReadModels;

namespace Wms.Notifications.Persistence;

// Read port inbox in app. AsNoTracking
internal sealed class NotificationInboxReader(NotificationsDbContext context) : INotificationInboxReader
{
    public async Task<InboxSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await context.Set<NotificationDelivery>().AsNoTracking()
            .CountAsync(
                delivery => delivery.UserId == userId
                    && delivery.Channel == Channel.InApp
                    && delivery.State != DeliveryState.Read,
                cancellationToken);
        return new InboxSummaryDto(unread);
    }

    public async Task<PagedResult<InboxItemDto>> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<NotificationDelivery> query = context.Set<NotificationDelivery>().AsNoTracking()
            .Where(delivery => delivery.UserId == userId && delivery.Channel == Channel.InApp);
        if (unreadOnly)
        {
            query = query.Where(delivery => delivery.State != DeliveryState.Read);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(delivery => delivery.CreatedAt)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<InboxItemDto>([.. items.Select(Map)], total, currentPage, size);
    }

    private static InboxItemDto Map(NotificationDelivery delivery) =>
        new(
            delivery.Id.Value,
            delivery.Title,
            delivery.Body,
            delivery.EventType,
            delivery.State == DeliveryState.Read,
            delivery.CreatedAt);
}
