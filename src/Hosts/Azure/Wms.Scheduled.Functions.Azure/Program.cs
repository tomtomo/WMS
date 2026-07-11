using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inventory.Application.Features.DetectNearExpiry;
using Wms.Platform.Azure.Scheduling;

// Function modul Inventory dan adapter Azure untuk menjalankan expiry scan serta menangani event.
// Scheduler didaftarkan sebagai kelas konkret agar timer trigger dapat membaca katalog job yang sama.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationBuildingBlocks(typeof(DetectNearExpiryCommand).Assembly);

        // Worker tidak memiliki HttpContext, jadi proses audit dijalankan sebagai SYSTEM.
        services.AddSystemCurrentUser();
        services.AddBuildingBlocksInfrastructure("wms-scheduled");
        services.AddInventoryModule(context.Configuration);

        // Daftarkan scheduler lebih dulu agar port dan katalog menggunakan instance yang sama.
        services.TryAddSingleton<FunctionsTimerRecurringJobScheduler>();
        services.TryAddSingleton<IRecurringJobScheduler>(provider =>
            provider.GetRequiredService<FunctionsTimerRecurringJobScheduler>());

        services.AddAzurePlatform(context.Configuration);
    })
    .Build();

await host.RunAsync();
