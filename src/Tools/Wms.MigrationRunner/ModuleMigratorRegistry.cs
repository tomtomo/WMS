using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Inbound.Infrastructure;
using Wms.Inventory.Infrastructure;
using Wms.MasterData.Infrastructure;
using Wms.MasterData.Infrastructure.Seed;
using Wms.Outbound.Infrastructure;

namespace Wms.MigrationRunner;

internal static class ModuleMigratorRegistry
{
    public static IReadOnlyList<Func<IServiceProvider, DbContext>> ModuleDbContexts { get; } =
    [
        provider => provider.GetRequiredService<InboundDbContext>(),
        provider => provider.GetRequiredService<InventoryDbContext>(),
        provider => provider.GetRequiredService<OutboundDbContext>(),
        provider => provider.GetRequiredService<MasterDataDbContext>(),
    ];

    // Seed referensi non-transaksional (idempotent) per modul, dijalankan setelah migration.
    public static IReadOnlyList<Func<IServiceProvider, CancellationToken, Task>> ModuleSeeders { get; } =
    [
        (provider, cancellationToken) =>
            MasterDataSeeder.SeedAsync(provider.GetRequiredService<MasterDataDbContext>(), cancellationToken),
    ];
}
