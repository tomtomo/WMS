using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Application.Features.AllocateWave;
using Wms.Inventory.Application.Features.DetectNearExpiry;
using Wms.Inventory.Application.Features.FulfillReservation;
using Wms.Inventory.Application.Features.ReceiveGoodsReceipt;
using Wms.Inventory.Application.Features.RemovePickedStock;
using Wms.Inventory.Infrastructure;
using Wms.Inventory.Infrastructure.Persistence;

namespace Microsoft.Extensions.DependencyInjection;

// Composition modul Inventory.
public static class InventoryModuleServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "wms")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InventoryDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' untuk modul Inventory tidak ditemukan.");

            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", InventoryDbContext.Schema);
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .UseSnakeCaseNamingConvention();

            // MigrationRunner tidak punya ICurrentUser — audit interceptor hanya dipasang bila tersedia.
            var auditableInterceptor = provider.GetService<AuditableInterceptor>();
            if (auditableInterceptor is not null)
            {
                options.AddInterceptors(auditableInterceptor);
            }
        });

        // UnitOfWork/Outbox/InboxGuard resolve DbContext non generik lewat context modul.
        // Hosting: satu host aktif = satu modul
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<InventoryDbContext>());

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        services.AddScoped<IPutawayTaskRepository, PutawayTaskRepository>();
        services.AddScoped<IStockReader, StockReader>();
        services.AddScoped<IStockReservationReader, StockReservationReader>();
        services.AddScoped<IPutawayTaskReader, PutawayTaskReader>();

        // Consumer receiving
        services.AddScoped<ReceiveGoodsReceiptHandler>();
        services.AddScoped<GRConfirmedConsumer>();

        // Consumer alokasi (WaveReleased)
        services.AddScoped<AllocateWaveHandler>();
        services.AddScoped<WaveReleasedConsumer>();

        // Consumer picking (PickingCompleted)
        services.AddScoped<FulfillReservationHandler>();
        services.AddScoped<PickingCompletedConsumer>();

        // Consumer dispatch (ShipmentDispatched)
        services.AddScoped<RemovePickedStockHandler>();
        services.AddScoped<ShipmentDispatchedConsumer>();

        // Scheduler Expiry (DetectNearExpiry)
        services.AddOptions<InventoryExpiryOptions>();
        services.AddScoped<DetectNearExpiryJob>();
        services.AddHostedService<InventoryRecurringJobsRegistrar>();

        // Kebijakan penempatan receiving — placeholder
        services.AddOptions<InventoryReceivingOptions>();
        services.AddScoped<IReceivingPolicy, DefaultReceivingPolicy>();

        return services;
    }
}
