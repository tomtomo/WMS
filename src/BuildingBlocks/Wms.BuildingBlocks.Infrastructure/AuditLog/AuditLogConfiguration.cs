using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wms.BuildingBlocks.Infrastructure.AuditLog;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogRecord>
{
    public void Configure(EntityTypeBuilder<AuditLogRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("audit_log", "infrastructure");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id");
        builder.Property(record => record.Actor).HasColumnName("actor").HasMaxLength(200);
        builder.Property(record => record.Action).HasColumnName("action").HasMaxLength(200);
        builder.Property(record => record.Entity).HasColumnName("entity").HasMaxLength(200);
        builder.Property(record => record.OccurredAt).HasColumnName("occurred_at");
        builder.Property(record => record.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
        builder.Property(record => record.Payload).HasColumnName("payload");
    }
}
