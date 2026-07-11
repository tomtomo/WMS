using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wms.Reporting.Persistence;

// Function Reporting memakai modul, adapter Azure, dan rail dispatcher, sedangkan proses subscribe ditangani oleh trigger.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationBuildingBlocks(typeof(ReportingDbContext).Assembly);

        // Worker tidak memiliki HttpContext, jadi proses audit dijalankan sebagai SYSTEM.
        services.AddSystemCurrentUser();
        services.AddBuildingBlocksInfrastructure("wms-reporting");
        services.AddReportingModule(context.Configuration);
        services.AddAzurePlatform(context.Configuration);

        // Reporting hanya memproses event, sedangkan lifecycle message ditangani oleh ServiceBusTrigger.
        services.AddReportingRailConsumers();
        services.AddEventingRailDispatchOnly();
    })
    .Build();

await host.RunAsync();
