using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure.Inbox;

namespace Wms.Reporting.Persistence;

// DbContext modul Reporting — schema 'reporting'
public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
{
    public const string Schema = "reporting";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        // Inbox idempotent consumer
        modelBuilder.ApplyConfiguration(new InboxConfiguration());

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
    }
}
