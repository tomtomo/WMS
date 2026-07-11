namespace Wms.Platform.Shared.Notifications;

// Dibuat terpisah dari SDK agar adapter bisa ditest tanpa kredensial Firebase.
public sealed record FirebasePushMessage(string DeviceToken, string Title, string Body);
