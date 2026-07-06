using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wms.Reporting.Abstractions;
using Wms.Reporting.Consumers;
using Wms.Reporting.Persistence;
using Wms.Reporting.Projections;
using Wms.Reporting.Queries;
using Wms.Reporting.Rebuild;

namespace Microsoft.Extensions.DependencyInjection;

// Composition modul Reporting
public static class ReportingModuleServiceCollectionExtensions
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "wms")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ReportingDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' untuk modul Reporting tidak ditemukan.");

            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", ReportingDbContext.Schema))
                .UseSnakeCaseNamingConvention();
        });

        // Inbox/UnitOfWork resolve DbContext non generik lewat context modul (satu host = satu modul).
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<ReportingDbContext>());

        // Projection store port. Postgres (cloud = Cosmos/Firestore).
        services.AddScoped<IProjectionStore, PostgresProjectionStore>();

        // Projection apply logic
        services.AddScoped<StockOnHandProjection>();
        services.AddScoped<ReceivingSummaryProjection>();
        services.AddScoped<DispatchSummaryProjection>();
        services.AddScoped<OperatorActivityProjection>();

        // Consumer integration event
        services.AddScoped<ReceivingSummaryConsumer>();
        services.AddScoped<StockOnHandFromReceiptConsumer>();
        services.AddScoped<StockRemovedConsumer>();
        services.AddScoped<PutawayCompletedConsumer>();
        services.AddScoped<PickingCompletedConsumer>();

        // Rebuild/replay
        services.AddScoped<RebuildProjectionsHandler>();

        // Read ports
        services.AddScoped<IStockOnHandReader, StockOnHandReader>();
        services.AddScoped<IReceivingSummaryReader, ReceivingSummaryReader>();
        services.AddScoped<IDispatchSummaryReader, DispatchSummaryReader>();
        services.AddScoped<IOperatorActivityReader, OperatorActivityReader>();

        return services;
    }
}
