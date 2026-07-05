using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Inbound.Infrastructure;
using Wms.Inventory.Infrastructure;
using Wms.Outbound.Infrastructure;

namespace Wms.MigrationRunner;

internal static class ModuleMigratorRegistry
{
    public static IReadOnlyList<Func<IServiceProvider, DbContext>> ModuleDbContexts { get; } =
    [
        provider => provider.GetRequiredService<InboundDbContext>(),
        provider => provider.GetRequiredService<InventoryDbContext>(),
        provider => provider.GetRequiredService<OutboundDbContext>(),
    ];
}
