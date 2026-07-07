using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// DbContext minimal
internal sealed class RailInfraDbContext(DbContextOptions<RailInfraDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.AddInfrastructureTables();
    }
}
