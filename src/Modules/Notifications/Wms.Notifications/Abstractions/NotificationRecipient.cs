namespace Wms.Notifications.Abstractions;

// Detail kontak recipient untuk channel Email/Push.
public sealed record NotificationRecipient(Guid UserId, string Email, string DeviceToken);
