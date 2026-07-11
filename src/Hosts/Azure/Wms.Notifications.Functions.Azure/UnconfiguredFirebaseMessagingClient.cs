using Wms.Platform.Shared.Notifications;

namespace Wms.Notifications.Functions.Azure;

// Digunakan saat Firebase belum dikonfigurasi agar pengiriman push gagal dengan pesan yang jelas.
// Kegagalan ini kemudian ditangani oleh dispatcher, bukan dianggap berhasil.
internal sealed class UnconfiguredFirebaseMessagingClient : IFirebaseMessagingClient
{
    public Task<string> SendAsync(FirebasePushMessage message, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(
            "FCM belum dikonfigurasi: set 'Firebase:ServiceAccountJson' (service-account JSON) untuk mengaktifkan channel push.");
}
