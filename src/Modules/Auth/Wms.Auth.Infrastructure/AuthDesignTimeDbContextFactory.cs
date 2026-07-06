using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wms.Auth.Infrastructure;

// Factory design time untuk 'dotnet ef migrations add'.
internal sealed class AuthDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=wms_design;Username=design_time_only",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", AuthDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options);
}
