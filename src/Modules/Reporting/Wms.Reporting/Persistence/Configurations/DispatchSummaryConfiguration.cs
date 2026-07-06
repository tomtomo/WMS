using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Persistence.Configurations;

internal sealed class DispatchSummaryConfiguration : IEntityTypeConfiguration<DispatchSummary>
{
    public void Configure(EntityTypeBuilder<DispatchSummary> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("dispatch_summary");
        builder.HasKey(summary => new { summary.WarehouseId, summary.Period });
        builder.Property(summary => summary.DispatchedVolume).HasPrecision(18, 3);
    }
}
