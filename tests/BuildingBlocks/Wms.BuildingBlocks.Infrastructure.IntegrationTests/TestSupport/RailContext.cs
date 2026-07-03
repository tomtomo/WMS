using Microsoft.EntityFrameworkCore;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;

internal static class RailContext
{
    public static RailTestDbContext New(string connectionString) =>
        new(new DbContextOptionsBuilder<RailTestDbContext>().UseNpgsql(connectionString).Options);
}
