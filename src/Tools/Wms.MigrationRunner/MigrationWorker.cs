using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Wms.MigrationRunner;

// Worker sekali jalan: apply migration tiap DbContext modul lalu seed admin, lalu hentikan host
internal sealed class MigrationWorker(
    IServiceProvider services,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = services.CreateScope();

            var moduleDbContexts = ModuleMigratorRegistry.ModuleDbContexts;
            logger.LogInformation("MigrationRunner: {Count} module DbContext terdaftar.", moduleDbContexts.Count);

            foreach (var resolveDbContext in moduleDbContexts)
            {
                var dbContext = resolveDbContext(scope.ServiceProvider);
                await dbContext.Database.MigrateAsync(stoppingToken).ConfigureAwait(false);
                logger.LogInformation("Migration diterapkan: {DbContext}.", dbContext.GetType().Name);
            }

            await AdminSeeder.SeedAsync(configuration, logger, stoppingToken).ConfigureAwait(false);

            logger.LogInformation("MigrationRunner selesai — exit 0.");
        }
        catch (Exception ex)
        {
            // Boundary sekali jalan: apa pun yang gagal maka exit non zero supaya AppHost tahu migration tidak tuntas.
            logger.LogError(ex, "MigrationRunner gagal — exit 1.");
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
