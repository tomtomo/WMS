namespace Wms.Inventory.Application.Features.DetectNearExpiry;

// Konfigurasi expiry scan
public sealed class InventoryExpiryOptions
{
    public const string SectionName = "Inventory:Expiry";

    // Emit StockNearExpiry untuk balance dengan expiry ≤ hari ini, ThresholdDays.
    public int ThresholdDays { get; set; } = 30;

    // Cron scan (default harian 02:00). via Hangfire (Local) / timer-trigger / Cloud Scheduler.
    public string Cron { get; set; } = "0 2 * * *";
}
