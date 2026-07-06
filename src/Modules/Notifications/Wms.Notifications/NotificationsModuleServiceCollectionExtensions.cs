using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Persistence;
using Wms.Notifications.Subscriptions;

namespace Microsoft.Extensions.DependencyInjection;

// Registrasi service untuk modul Notifications.
public static class NotificationsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "wms")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<NotificationsDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{connectionStringName}' untuk modul Notifications tidak ditemukan.");

            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema))
                .UseSnakeCaseNamingConvention();

            // Pasang audit interceptor hanya jika tersedia.
            var auditableInterceptor = provider.GetService<AuditableInterceptor>();
            if (auditableInterceptor is not null)
            {
                options.AddInterceptors(auditableInterceptor);
            }
        });

        // Gunakan NotificationsDbContext sebagai DbContext utama modul ini.
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<NotificationsDbContext>());

        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.AddScoped<INotificationSubscriptionReader, NotificationSubscriptionReader>();
        services.AddScoped<INotificationInboxReader, NotificationInboxReader>();

        services.AddScoped<SubscriptionResolver>();
        services.AddScoped<NotificationFanout>();

        // Consumer integration event
        services.AddScoped<GoodsReceiptPendingReviewConsumer>();
        services.AddScoped<WaveReadyConsumer>();
        services.AddScoped<StockAllocationCompletedConsumer>();
        services.AddScoped<StockNearExpiryConsumer>();
        services.AddScoped<PutawayTaskAssignedConsumer>();
        services.AddScoped<PickingTaskAssignedConsumer>();

        // Service untuk memproses delivery notifikasi.
        services.AddScoped<DeliveryDispatcher>();
        services.AddSingleton<DeliveryDispatchRunner>();
        services.AddHostedService<DeliveryDispatcherWorker>();

        return services;
    }
}
