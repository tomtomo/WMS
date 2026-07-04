using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.EventTranslation;
using Wms.Inbound.Infrastructure;
using Wms.Inbound.Infrastructure.Persistence;

namespace Microsoft.Extensions.DependencyInjection;

// Composition modul Inbound.
public static class InboundModuleServiceCollectionExtensions
{
    public static IServiceCollection AddInboundModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "wms")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InboundDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' untuk modul Inbound tidak ditemukan.");

            options
                .UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", InboundDbContext.Schema);

                    // Aggregate membawa 7 owned collection
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

        // UnitOfWork/Outbox/AuditLogStore resolve DbContext non generik lewat context modul.
        // hosting: satu host aktif = satu modul
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<InboundDbContext>());

        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddScoped<IGRAttachmentRepository, GRAttachmentRepository>();
        services.AddScoped<IGoodsReceiptReader, GoodsReceiptReader>();
        services.AddScoped<IGoodsReceiptListReader, GoodsReceiptListReader>();
        services.AddScoped<IGRAttachmentReader, GRAttachmentReader>();
        services.AddScoped<GoodsReceiptEventTranslator>();

        return services;
    }
}
