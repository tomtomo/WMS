namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Simpan atau ambil hasil request per Idempotency Key sehingga POST yang di retry tidak dieksekusi dua kali
// (Postgres Local, Managed Redis Azure, Memorystore GCP).
public interface IApiIdempotencyStore
{
    Task<string?> GetResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task SaveResponseAsync(
        string idempotencyKey,
        string response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);
}
