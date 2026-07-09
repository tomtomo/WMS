using System.Diagnostics;
using AwesomeAssertions;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inventory.Infrastructure;
using Wms.Inventory.IntegrationTests.TestSupport;
using Wms.Platform.Local.Scheduling;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// Memastikan job expiry scan benar-benar dijalankan Hangfire server, lalu menghasilkan event StockNearExpiry di outbox.
[Collection(PostgresCollection.Name)]
public sealed class HangfireExpiryScanTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string NearExpiryLogicalName = "inventory.stock_near_expiry.v1";
    private static readonly DateTimeOffset _today = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(_today);
    private WebApplication _app = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:wms"] = connectionString,
        });

        InventoryTestHost.AddInventoryComposition(builder.Services, connectionString);
        builder.Services.AddSingleton<TimeProvider>(_clock);

        // Storage, server, dan scheduler Hangfire dibuat seperti host lokal. Interval polling dipercepat supaya test tidak perlu menunggu lama.
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                storage => storage.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions { QueuePollInterval = TimeSpan.FromMilliseconds(250) }));
        builder.Services.AddHangfireServer(options => options.SchedulePollingInterval = TimeSpan.FromMilliseconds(250));
        builder.Services.AddSingleton<IRecurringJobScheduler, HangfireRecurringJobScheduler>();

        _app = builder.Build();

        using (var scope = _app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
        }

        // Saat app start, expiry scan didaftarkan dan server Hangfire mulai memproses job.
        await _app.StartAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Expiry_scan_recurring_job_fires_end_to_end_via_hangfire_server()
    {
        await StockSeeder.SeedAvailableAsync(_app.Services, batch: "LOT-EXP", expiry: new DateOnly(2026, 1, 15));

        // Jalankan job sekarang agar test tidak bergantung pada jadwal cron.
        using (var scope = _app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IRecurringJobManager>().Trigger("inventory-expiry-scan");
        }

        var emitted = await WaitForOutboxAsync(NearExpiryLogicalName, TimeSpan.FromSeconds(30));

        emitted.Should().BeTrue("Hangfire server menjalankan expiry scan dan menulis StockNearExpiry ke outbox");
    }

    private async Task<bool> WaitForOutboxAsync(string logicalName, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var rows = await PipelineRunner.OutboxRowsAsync(_app.Services, logicalName);
            if (rows.Count > 0)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return false;
    }
}
