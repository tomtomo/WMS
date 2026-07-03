using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

// Test pengganti DbContext modul di composition
public sealed class CompositionRailDbContext(DbContextOptions<CompositionRailDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddInfrastructureTables();
}
