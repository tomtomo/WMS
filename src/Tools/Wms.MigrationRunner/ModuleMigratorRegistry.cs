using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Infrastructure;
using Wms.Auth.Infrastructure.Seed;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Inbound.Infrastructure;
using Wms.Inventory.Infrastructure;
using Wms.MasterData.Infrastructure;
using Wms.MasterData.Infrastructure.Seed;
using Wms.Notifications.Persistence;
using Wms.Notifications.Persistence.Seed;
using Wms.Outbound.Infrastructure;
using Wms.Reporting.Persistence;

namespace Wms.MigrationRunner;

internal static class ModuleMigratorRegistry
{
    public static IReadOnlyList<Func<IServiceProvider, DbContext>> ModuleDbContexts { get; } =
    [
        provider => provider.GetRequiredService<InboundDbContext>(),
        provider => provider.GetRequiredService<InventoryDbContext>(),
        provider => provider.GetRequiredService<OutboundDbContext>(),
        provider => provider.GetRequiredService<MasterDataDbContext>(),
        provider => provider.GetRequiredService<AuthDbContext>(),
        provider => provider.GetRequiredService<ReportingDbContext>(),
        provider => provider.GetRequiredService<NotificationsDbContext>(),
    ];

    // Daftar seeder untuk setiap modul.
    public static IReadOnlyList<Func<IServiceProvider, CancellationToken, Task>> ModuleSeeders { get; } =
    [
        (provider, cancellationToken) =>
            MasterDataSeeder.SeedAsync(provider.GetRequiredService<MasterDataDbContext>(), cancellationToken),
        (provider, cancellationToken) =>
            AuthSeeder.SeedAsync(
                provider.GetRequiredService<AuthDbContext>(),
                provider.GetRequiredService<IPasswordHasher>(),
                cancellationToken),
        (provider, cancellationToken) =>
            NotificationSubscriptionSeeder.SeedAsync(
                provider.GetRequiredService<NotificationsDbContext>(), cancellationToken),
    ];
}
