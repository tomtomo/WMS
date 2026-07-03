using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wms.BuildingBlocks.Infrastructure.Outbox;

public sealed class OutboxConfiguration : IEntityTypeConfiguration<OutboxRecord>
{
    public void Configure(EntityTypeBuilder<OutboxRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("outbox", "infrastructure");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id");
        builder.Property(record => record.LogicalName).HasColumnName("logical_name").HasMaxLength(200);
        builder.Property(record => record.DeliveryClass).HasColumnName("delivery_class")
            .HasConversion<string>().HasMaxLength(32);
        builder.Property(record => record.OccurredAt).HasColumnName("occurred_at");
        builder.Property(record => record.Payload).HasColumnName("payload");
        builder.Property(record => record.Traceparent).HasColumnName("traceparent").HasMaxLength(64);
        builder.Property(record => record.Tracestate).HasColumnName("tracestate").HasMaxLength(256);
        builder.Property(record => record.ProcessedAt).HasColumnName("processed_at");
        builder.Property(record => record.AttemptCount).HasColumnName("attempt_count");
    }
}
