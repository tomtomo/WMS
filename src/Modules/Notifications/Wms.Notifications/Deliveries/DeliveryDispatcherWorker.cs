using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Notifications.Deliveries;

// Worker untuk memproses delivery notifikasi yang masih pending.
internal sealed class DeliveryDispatcherWorker(
    IServiceScopeFactory scopeFactory,
    DeliveryDispatchRunner runner,
    ILogger<DeliveryDispatcherWorker> logger,
    TimeProvider timeProvider)
    : BackgroundService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Lewati worker jika notifier belum didaftarkan.
        using (var probe = scopeFactory.CreateScope())
        {
            if (probe.ServiceProvider.GetService<IInAppNotifier>() is null)
            {
                return;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await runner.DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
#pragma warning disable S2221
            catch (Exception ex)
            {
                logger.LogError(ex, "Dispatch notifikasi gagal pada satu poll, lanjut cycle berikutnya.");
            }
#pragma warning restore S2221

            try
            {
                await Task.Delay(_pollInterval, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
