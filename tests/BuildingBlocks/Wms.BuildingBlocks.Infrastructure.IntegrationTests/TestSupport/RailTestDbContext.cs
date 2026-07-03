using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure;
using Wms.BuildingBlocks.Infrastructure.Persistence;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestSupport;

public sealed class RailTestDbContext(DbContextOptions<RailTestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInfrastructureTables();

        modelBuilder.Entity<WidgetEntity>(entity =>
        {
            entity.ToTable("widget");
            entity.HasKey(widget => widget.Id);
            entity.Property(widget => widget.Name).HasMaxLength(200);
            entity.UseXminConcurrencyToken();
        });

        modelBuilder.Entity<AuditableWidget>(entity =>
        {
            entity.ToTable("auditable_widget");
            entity.HasKey(widget => widget.Id);
            entity.Property(widget => widget.Name).HasMaxLength(200);
            entity.Property(widget => widget.CreatedBy).HasMaxLength(200);
            entity.Property(widget => widget.ModifiedBy).HasMaxLength(200);
        });
    }
}
