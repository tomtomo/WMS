namespace Wms.WebUI.Services;

// DTO modul Notifications. Dipisah dari NotificationsApi supaya file typed-client fokus ke daftar endpoint.
public sealed record InboxItemDto(Guid DeliveryId, string Title, string Body, string EventType, bool IsRead, DateTimeOffset CreatedAt);
