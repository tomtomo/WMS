namespace Wms.Notifications.ReadModels;

// Satu item inbox in app untuk WebUI.
public sealed record InboxItemDto(
    Guid DeliveryId,
    string Title,
    string Body,
    string EventType,
    bool IsRead,
    DateTimeOffset CreatedAt);
