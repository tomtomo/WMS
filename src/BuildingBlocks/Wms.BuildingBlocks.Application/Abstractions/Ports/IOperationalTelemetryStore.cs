namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Simpan telemetry operasional sebagai data append-only dan ambil data terbaru per gudang.
// Local memakai PostgreSQL, sedangkan Azure memakai Cosmos dengan masa simpan 7 hari.
public interface IOperationalTelemetryStore
{
    Task AppendAsync(OperationalTelemetryRecord record, CancellationToken cancellationToken = default);

    // Batasi window di server agar query tidak mengambil data terlalu banyak.
    Task<IReadOnlyList<OperationalTelemetryRecord>> GetRecentAsync(
        Guid warehouseId,
        TimeSpan window,
        CancellationToken cancellationToken = default);
}
