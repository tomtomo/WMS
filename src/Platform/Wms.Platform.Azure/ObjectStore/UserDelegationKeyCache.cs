using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Wms.Platform.Azure.ObjectStore;

// User delegation key dicache agar pembuatan URL tidak perlu selalu meminta token ke Entra dan key baru ke Storage.
internal sealed class UserDelegationKeyCache(
    BlobServiceClient serviceClient,
    TimeProvider timeProvider,
    TimeSpan keyLifetime,
    TimeSpan clockSkew)
{
    // Update key sebelum masa berlakunya habis agar URL baru tidak memakai key yang sudah kedaluwarsa.
    private static readonly TimeSpan _refreshMargin = TimeSpan.FromMinutes(5);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    // Simpan key dan masa berlakunya dalam satu objek agar nilainya tetap konsisten saat dibaca dari beberapa thread.
    private volatile CachedKey? _cached;

    public UserDelegationKey Get()
    {
        var snapshot = _cached;
        if (IsUsable(snapshot))
        {
            return snapshot!.Key;
        }

        _refreshLock.Wait();
        try
        {
            snapshot = _cached;
            if (IsUsable(snapshot))
            {
                return snapshot!.Key;
            }

            var now = timeProvider.GetUtcNow();
            var expiresOn = now + keyLifetime;

            // CreateReadUrl bersifat sinkron, sedangkan pengambilan user delegation key membutuhkan akses jaringan.
            // Blocking ini hanya terjadi saat key belum tersedia atau perlu diupdate, bukan pada setiap request.
            var key = serviceClient.GetUserDelegationKey(now - clockSkew, expiresOn).Value;
            _cached = new CachedKey(key, expiresOn);
            return key;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool IsUsable(CachedKey? cached) =>
        cached is not null && timeProvider.GetUtcNow() + _refreshMargin < cached.ExpiresAt;

    private sealed record CachedKey(UserDelegationKey Key, DateTimeOffset ExpiresAt);
}
