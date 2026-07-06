using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Notifications.ReadModels;

namespace Wms.Notifications.Abstractions;

// Read port in app inbox, AsNoTracking, untuk WebUI
public interface INotificationInboxReader : IReader
{
    Task<InboxSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<InboxItemDto>> ListAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);
}
