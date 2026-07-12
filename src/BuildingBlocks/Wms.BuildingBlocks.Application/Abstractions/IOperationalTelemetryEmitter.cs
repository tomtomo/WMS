namespace Wms.BuildingBlocks.Application.Abstractions;

// Kirim telemetry operasional dari handler bisnis tanpa mengganggu proses utama jika pengiriman gagal.
// Interface ini juga menentukan nama stream dan aturan pengirimannya.
public interface IOperationalTelemetryEmitter
{
    Task EmitAsync(OperationalTelemetryRecord record, CancellationToken cancellationToken = default);
}
