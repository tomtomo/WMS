namespace Wms.Notifications.Consumers;

// Konten notifikasi yang sudah siap dikirim.
public sealed record NotificationContent(string Title, string Body, string SourceEventType);
