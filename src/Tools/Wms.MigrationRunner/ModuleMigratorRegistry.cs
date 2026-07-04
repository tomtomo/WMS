using Microsoft.EntityFrameworkCore;

namespace Wms.MigrationRunner;

internal static class ModuleMigratorRegistry
{
    public static IReadOnlyList<Func<IServiceProvider, DbContext>> ModuleDbContexts { get; } = [];
}
