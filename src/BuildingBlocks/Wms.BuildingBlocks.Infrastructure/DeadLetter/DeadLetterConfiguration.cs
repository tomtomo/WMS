using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wms.BuildingBlocks.Infrastructure.DeadLetter;

public sealed class DeadLetterConfiguration : IEntityTypeConfiguration<DeadLetterRecord>
{
    public void Configure(EntityTypeBuilder<DeadLetterRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("dead_letter", "infrastructure");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id");
        builder.Property(record => record.Source).HasColumnName("source").HasMaxLength(200);
        builder.Property(record => record.Payload).HasColumnName("payload");
        builder.Property(record => record.Error).HasColumnName("error");
        builder.Property(record => record.AttemptCount).HasColumnName("attempt_count");
        builder.Property(record => record.DeadLetteredAt).HasColumnName("dead_lettered_at");
    }
}
