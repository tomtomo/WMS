using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Infrastructure;
using Wms.MasterData.Infrastructure.Persistence;
using Wms.MasterData.Infrastructure.Persistence.Cached;

namespace Microsoft.Extensions.DependencyInjection;

// Composition modul MasterData.
public static class MasterDataModuleServiceCollectionExtensions
{
    public static IServiceCollection AddMasterDataModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "wms")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<MasterDataDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' untuk modul MasterData tidak ditemukan.");

            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", MasterDataDbContext.Schema))
                .UseSnakeCaseNamingConvention();

            // MigrationRunner tak punya ICurrentUser — audit interceptor hanya dipasang jika tersedia.
            var auditableInterceptor = provider.GetService<AuditableInterceptor>();
            if (auditableInterceptor is not null)
            {
                options.AddInterceptors(auditableInterceptor);
            }
        });

        // UnitOfWork/Outbox/AuditLogStore resolve DbContext non-generik lewat context modul (satu host = satu modul).
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<MasterDataDbContext>());

        // Write side repositories.
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Read side: EF reader dan Decorator cache aside
        // host/platform wajib mendaftarkan ICacheStore — adapter InMemory Local, Redis Azure, atau Memorystore GCP.
        services.AddScoped<WarehouseReader>();
        services.AddScoped<IWarehouseReader>(provider =>
            new CachedWarehouseReader(provider.GetRequiredService<WarehouseReader>(), provider.GetRequiredService<ICacheStore>()));

        services.AddScoped<LocationReader>();
        services.AddScoped<ILocationReader>(provider =>
            new CachedLocationReader(provider.GetRequiredService<LocationReader>(), provider.GetRequiredService<ICacheStore>()));

        services.AddScoped<ProductReader>();
        services.AddScoped<IProductReader>(provider =>
            new CachedProductReader(provider.GetRequiredService<ProductReader>(), provider.GetRequiredService<ICacheStore>()));

        return services;
    }
}
