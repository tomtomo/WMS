namespace Wms.Platform.Shared.Notifications;

// Wrapper untuk FirebaseMessaging agar adapter push bisa ditest tanpa membuat instance SDK dan menyiapkan kredensial.
public interface IFirebaseMessagingClient
{
    // Mengembalikan ID pesan dari FCM sebagai bukti bahwa request pengiriman sudah diterima
    Task<string> SendAsync(FirebasePushMessage message, CancellationToken cancellationToken = default);
}
