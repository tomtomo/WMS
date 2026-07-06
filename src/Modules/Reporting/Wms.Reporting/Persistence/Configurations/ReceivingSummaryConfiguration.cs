using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Persistence.Configurations;

internal sealed class ReceivingSummaryConfiguration : IEntityTypeConfiguration<ReceivingSummary>
{
    public void Configure(EntityTypeBuilder<ReceivingSummary> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("receiving_summary");
        builder.HasKey(summary => new { summary.SupplierId, summary.Period });
        builder.Property(summary => summary.ReceivedQty).HasPrecision(18, 3);
    }
}
