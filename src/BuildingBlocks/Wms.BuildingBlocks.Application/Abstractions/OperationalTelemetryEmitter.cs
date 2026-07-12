using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Application.Abstractions;

// Kirim telemetry operasional melalui event stream tanpa menggagalkan transaksi bisnis jika pengiriman bermasalah.
internal sealed class OperationalTelemetryEmitter(
    IEventStreamPublisher publisher,
    ILogger<OperationalTelemetryEmitter> logger) : IOperationalTelemetryEmitter
{
    public async Task EmitAsync(OperationalTelemetryRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            await publisher.PublishAsync(OperationalTelemetryStream.Name, record, cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031, S2221 // Semua error dari event stream sengaja diabaikan agar proses bisnis tetap berhasil.
        catch (Exception exception)
#pragma warning restore CA1031, S2221
        {
            logger.LogWarning(exception, "Emit telemetry operasional ke stream {Stream} gagal, dilewati.", OperationalTelemetryStream.Name);
        }
    }
}
