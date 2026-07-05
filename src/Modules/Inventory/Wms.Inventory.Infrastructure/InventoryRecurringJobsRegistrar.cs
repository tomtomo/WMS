using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inventory.Application.Features.DetectNearExpiry;

namespace Wms.Inventory.Infrastructure;

// Startup hook modul: daftarkan expiry scan ke scheduler cron
internal sealed class InventoryRecurringJobsRegistrar(
    IServiceProvider serviceProvider,
    IOptions<InventoryExpiryOptions> options,
    ILogger<InventoryRecurringJobsRegistrar> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scheduler = serviceProvider.GetService<IRecurringJobScheduler>();
        if (scheduler is null)
        {
            logger.LogInformation("IRecurringJobScheduler tak tersedia; expiry scan tidak dijadwalkan di host ini.");
            return;
        }

        await scheduler.ScheduleRecurringAsync<DetectNearExpiryJob>(
            "inventory-expiry-scan", options.Value.Cron, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
