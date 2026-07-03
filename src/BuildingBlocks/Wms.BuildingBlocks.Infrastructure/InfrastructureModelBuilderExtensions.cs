using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure.AuditLog;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.BuildingBlocks.Infrastructure.Inbox;
using Wms.BuildingBlocks.Infrastructure.Outbox;

namespace Wms.BuildingBlocks.Infrastructure;

// Dipanggil di OnModelCreating tiap modul
public static class InfrastructureModelBuilderExtensions
{
    public static void AddInfrastructureTables(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new OutboxConfiguration());
        modelBuilder.ApplyConfiguration(new InboxConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
    }
}
