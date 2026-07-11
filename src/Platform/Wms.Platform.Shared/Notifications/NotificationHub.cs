using Microsoft.AspNetCore.SignalR;

namespace Wms.Platform.Shared.Notifications;

// Hub notifikasi in app masih di-host langsung tanpa Azure SignalR Service.
// Kontrak client didefinisikan di sini, sedangkan endpoint hub dipasang oleh host.
public sealed class NotificationHub : Hub
{
    public const string ReceiveNotificationMethod = "ReceiveNotification";
}
