using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Persistence.Configurations;

internal sealed class StockOnHandViewConfiguration : IEntityTypeConfiguration<StockOnHandView>
{
    public void Configure(EntityTypeBuilder<StockOnHandView> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("stock_on_hand_view");
        builder.HasKey(view => new { view.WarehouseId, view.Sku, view.Batch });
        builder.Property(view => view.Sku).HasMaxLength(64);
        builder.Property(view => view.Batch).HasMaxLength(64);
        builder.Property(view => view.QtyOnHand).HasPrecision(18, 3);
    }
}
