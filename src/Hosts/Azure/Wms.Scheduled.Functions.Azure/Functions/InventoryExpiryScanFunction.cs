using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Platform.Azure.Scheduling;

namespace Wms.Scheduled.Functions.Azure;

// Timer trigger menjalankan job yang sudah terdaftar di katalog Application.
// Job tetap berada di Application, sedangkan trigger ini hanya menjadi pemicu berdasarkan cron.
public sealed class InventoryExpiryScanFunction(
    FunctionsTimerRecurringJobScheduler scheduler,
    IServiceScopeFactory scopeFactory)
{
    private const string JobId = "inventory-expiry-scan";

    // Value cron ini harus sama dengan Inventory__Expiry__Cron karena atribut TimerTrigger memerlukan konstanta.
    private const string Schedule = "0 0 2 * * *";

    [Function("InventoryExpiryScan")]
    public async Task RunAsync([TimerTrigger(Schedule)] TimerInfo timer, CancellationToken cancellationToken)
    {
        // Ambil tipe job dari katalog dan hentikan proses jika registrar Inventory belum mendaftarkannya.
        var registration = scheduler.Jobs.SingleOrDefault(job => job.JobId == JobId)
            ?? throw new InvalidOperationException(
                $"Job '{JobId}' tidak terdaftar di katalog scheduler — registrar modul Inventory tidak jalan.");

        using var scope = scopeFactory.CreateScope();
        var job = (IRecurringJob)ActivatorUtilities.CreateInstance(scope.ServiceProvider, registration.JobType);
        await job.ExecuteAsync(cancellationToken);
    }
}
