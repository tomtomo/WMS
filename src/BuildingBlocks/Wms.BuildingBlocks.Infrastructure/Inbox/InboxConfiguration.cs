using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wms.BuildingBlocks.Infrastructure.Inbox;

public sealed class InboxConfiguration : IEntityTypeConfiguration<InboxRecord>
{
    public void Configure(EntityTypeBuilder<InboxRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("inbox", "infrastructure");
        builder.HasKey(record => new { record.EventId, record.HandlerType });
        builder.Property(record => record.EventId).HasColumnName("event_id");
        builder.Property(record => record.HandlerType).HasColumnName("handler_type").HasMaxLength(200);
        builder.Property(record => record.ProcessedAt).HasColumnName("processed_at");
    }
}
