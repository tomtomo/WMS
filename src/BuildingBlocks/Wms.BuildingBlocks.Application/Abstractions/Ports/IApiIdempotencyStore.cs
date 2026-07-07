using Wms.BuildingBlocks.Application.Abstractions;

namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Store idempotency dengan pola reserve then fill agar request duplikat tidak dieksekusi dua kali.
public interface IApiIdempotencyStore
{
    Task<IdempotencyReservation> TryReserveAsync(
        string idempotencyKey,
        TimeSpan pendingTimeToLive,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        string idempotencyKey,
        Guid ownerToken,
        string response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);

    // Lepas klaim pending saat eksekusi gagal agar retry berikutnya boleh eksekusi ulang.
    Task ReleaseAsync(string idempotencyKey, Guid ownerToken, CancellationToken cancellationToken = default);
}
