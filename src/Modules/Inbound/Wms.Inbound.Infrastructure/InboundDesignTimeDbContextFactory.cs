using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wms.Inbound.Infrastructure;

// Factory design time 'dotnet ef migrations add'
internal sealed class InboundDesignTimeDbContextFactory : IDesignTimeDbContextFactory<InboundDbContext>
{
    public InboundDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<InboundDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=wms_design;Username=design_time_only",
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", InboundDbContext.Schema);
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
            .UseSnakeCaseNamingConvention()
            .Options);
}
