using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wms.Outbound.Infrastructure;

// Factory design time 'dotnet ef migrations add'
internal sealed class OutboundDesignTimeDbContextFactory : IDesignTimeDbContextFactory<OutboundDbContext>
{
    public OutboundDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OutboundDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=wms_design;Username=design_time_only",
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", OutboundDbContext.Schema);
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
            .UseSnakeCaseNamingConvention()
            .Options);
}
