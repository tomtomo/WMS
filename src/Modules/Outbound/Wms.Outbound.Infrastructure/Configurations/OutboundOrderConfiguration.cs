using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.ValueObjects;

namespace Wms.Outbound.Infrastructure.Configurations;

// Mapping aggregate OutboundOrder (demand customer, multi-SKU).
public sealed class OutboundOrderConfiguration : IEntityTypeConfiguration<OutboundOrder>
{
    private const int SkuMaxLength = 64;

    private const int UomMaxLength = 16;

    private const int EnumMaxLength = 24;

    public void Configure(EntityTypeBuilder<OutboundOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("outbound_order");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id)
            .HasConversion(id => id.Value, value => OutboundOrderId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(order => order.CustomerId);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);

        // waveId kosong saat backlog (New), diclear saat kembali ke backlog.
        builder.Property(order => order.WaveId)
            .HasConversion(id => id!.Value, value => WaveId.Create(value).Value);

        builder.Property(order => order.CreatedBy).HasMaxLength(200);
        builder.Property(order => order.ModifiedBy).HasMaxLength(200);

        // Backlog query per status dan antrean order per wave.
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.WaveId);

        builder.UseXminConcurrencyToken();

        // Alamat tujuan — VO
        builder.OwnsOne(order => order.ShipTo, shipTo =>
        {
            shipTo.Property(address => address.Recipient).HasMaxLength(200);
            shipTo.Property(address => address.AddressLine).HasMaxLength(400);
            shipTo.Property(address => address.City).HasMaxLength(120);
        });

        // Demand per SKU
        builder.OwnsMany(order => order.OrderLines, line =>
        {
            line.ToTable("outbound_order_line");
            line.WithOwner().HasForeignKey("OutboundOrderId");
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.Property(l => l.Sku).HasMaxLength(SkuMaxLength);
            line.Property(l => l.Qty).HasPrecision(18, 3);
            line.Property(l => l.Uom)
                .HasConversion(uom => uom.Value, value => Uom.Create(value).Value)
                .HasMaxLength(UomMaxLength);
            line.Property(l => l.AllocatedQty).HasPrecision(18, 3);
            line.Property(l => l.AllocationStatus).HasConversion<string>().HasMaxLength(EnumMaxLength);
        });

        // Sisa demand Partial/Short — rewaveable
        builder.OwnsMany(order => order.Backorders, backorder =>
        {
            backorder.ToTable("outbound_backorder");
            backorder.WithOwner().HasForeignKey("OutboundOrderId");
            backorder.Property<int>("Id").ValueGeneratedOnAdd();
            backorder.HasKey("Id");
            backorder.Property(b => b.Sku).HasMaxLength(SkuMaxLength);
            backorder.Property(b => b.ShortQty).HasPrecision(18, 3);
        });
    }
}
