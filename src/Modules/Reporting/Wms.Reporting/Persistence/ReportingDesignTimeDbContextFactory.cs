using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wms.Reporting.Persistence;

// Factory design time untuk 'dotnet ef migrations add'
internal sealed class ReportingDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ReportingDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=wms_design;Username=design_time_only",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ReportingDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options);
}
