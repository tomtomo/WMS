using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Streaming;

// Di Local, worker membaca telemetry dari stream in memory lalu menyimpannya ke hot store.
// Di Azure, proses yang sama ditangani oleh EventHubTrigger.
public sealed class OperationalTelemetryStreamWorker(
    IEventStreamConsumer consumer,
    IOperationalTelemetryStore store,
    ILogger<OperationalTelemetryStreamWorker> logger) : BackgroundService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

    internal Task DrainOnceAsync(CancellationToken cancellationToken) =>
        consumer.ConsumeAsync<OperationalTelemetryRecord>(
            OperationalTelemetryStream.Name,
            (record, token) => store.AppendAsync(record, token),
            cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainOnceAsync(stoppingToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031, S2221
            catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031, S2221
            {
                logger.LogWarning(exception, "Gagal menyimpan telemetry operasional, proses akan dicoba kembali.");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
