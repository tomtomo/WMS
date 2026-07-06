using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wms.Notifications.Persistence;

// Factory design time untuk 'dotnet ef migrations add'.
internal sealed class NotificationsDesignTimeDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=wms_design;Username=design_time_only",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options);
}
