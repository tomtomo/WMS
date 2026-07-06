using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure;

namespace Wms.Notifications.Persistence;

// DbContext modul Notifications — schema 'notifications'
public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public const string Schema = "notifications";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.AddInfrastructureTables();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
