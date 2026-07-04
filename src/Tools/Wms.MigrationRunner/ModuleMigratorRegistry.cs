using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Inbound.Infrastructure;

namespace Wms.MigrationRunner;

internal static class ModuleMigratorRegistry
{
    public static IReadOnlyList<Func<IServiceProvider, DbContext>> ModuleDbContexts { get; } =
    [
        provider => provider.GetRequiredService<InboundDbContext>(),
    ];
}
