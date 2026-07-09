using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.EventTranslation;
using Wms.Outbound.Application.Features.HandleStockAllocationCompleted;
using Wms.Outbound.Infrastructure;
using Wms.Outbound.Infrastructure.Persistence;
using Wms.Outbound.Infrastructure.Saga;

namespace Microsoft.Extensions.DependencyInjection;

// Composition modul Outbound.
public static class OutboundModuleServiceCollectionExtensions
{
    public static IServiceCollection AddOutboundModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "wms")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<OutboundDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' untuk modul Outbound tidak ditemukan.");

            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", OutboundDbContext.Schema);
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

        // UnitOfWork/Outbox/InboxGuard resolve DbContext non-generik lewat context modul.
        // Hosting: satu host aktif = satu modul.
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<OutboundDbContext>());

        services.AddScoped<IWaveRepository, WaveRepository>();
        services.AddScoped<IOutboundOrderRepository, OutboundOrderRepository>();
        services.AddScoped<IPickingTaskRepository, PickingTaskRepository>();

        // Read port (CQRS ringan) — AsNoTracking ke read DTO.
        services.AddScoped<WaveReader>();
        services.AddScoped<IWaveReader>(provider => provider.GetRequiredService<WaveReader>());
        services.AddScoped<IWaveListReader>(provider => provider.GetRequiredService<WaveReader>());
        services.AddScoped<IOutboundOrderReader, OutboundOrderReader>();
        services.AddScoped<IPickingTaskReader, PickingTaskReader>();

        // Terjemahan domain ke integration event ke Outbox.
        services.AddScoped<OutboundEventTranslator>();

        // Consumer alokasi (StockAllocationCompleted)
        services.AddScoped<HandleStockAllocationCompletedHandler>();
        services.AddScoped<StockAllocationCompletedConsumer>();

        // Konfigurasi picking tetap bisa dibind meski guid policy belum diisi.
        // Nilai defaultnya akan dijaga saat policy benar-benar dipakai.
        services.AddValidatedOptions<OutboundPickingOptions>(configuration, OutboundPickingOptions.SectionName);
        services.AddScoped<IPickAssignmentPolicy, DefaultPickAssignmentPolicy>();

        // Saga orchestrator in proc, state di DB Outbound — reserved orchestrated compensation.
        services.AddScoped<ISagaOrchestrator, OutboundSagaOrchestrator>();

        return services;
    }
}
