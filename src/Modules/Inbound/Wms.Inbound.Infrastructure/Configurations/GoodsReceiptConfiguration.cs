using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Infrastructure.Configurations;

// Mapping aggregate GoodsReceipt
public sealed class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    private const int SkuMaxLength = 64;

    private const int EnumMaxLength = 24;

    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("goods_receipt");

        builder.HasKey(gr => gr.Id);
        builder.Property(gr => gr.Id)
            .HasConversion(id => id.Value, value => GoodsReceiptId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(gr => gr.PoRef).HasMaxLength(64);
        builder.Property(gr => gr.SupplierId);
        builder.Property(gr => gr.WarehouseId);
        builder.Property(gr => gr.DockDoor)
            .HasConversion(dockDoor => dockDoor.Value, value => DockDoor.Create(value).Value)
            .HasMaxLength(32);
        builder.Property(gr => gr.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(gr => gr.HoldReason)
            .HasConversion(reason => reason!.Value, value => HoldReason.Create(value).Value)
            .HasMaxLength(500);

        builder.Property(gr => gr.CreatedBy).HasMaxLength(200);
        builder.Property(gr => gr.ModifiedBy).HasMaxLength(200);

        // Antrean review SPV query by status.
        builder.HasIndex(gr => gr.Status);

        builder.UseXminConcurrencyToken();

        MapOwnedLines(builder);
    }

    private static void MapOwnedLines(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.OwnsMany(gr => gr.ExpectedLines, line =>
        {
            line.ToTable("gr_expected_line");
            line.WithOwner().HasForeignKey("GoodsReceiptId");
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.Property(l => l.Sku).HasMaxLength(SkuMaxLength);
            line.Property(l => l.ExpectedQty).HasPrecision(18, 3);
            line.Property(l => l.Uom).HasMaxLength(16);
        });

        builder.OwnsMany(gr => gr.ScannedLines, line =>
        {
            line.ToTable("gr_scanned_line");
            line.WithOwner().HasForeignKey("GoodsReceiptId");
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.Property(l => l.Sku).HasMaxLength(SkuMaxLength);
            line.Property(l => l.ActualQty).HasPrecision(18, 3);
            line.Property(l => l.Batch).HasMaxLength(64);

            // Get only tanpa setter tidak di map EF
            line.Property(l => l.Expiry);
            line.Property(l => l.LineStatus).HasConversion<string>().HasMaxLength(EnumMaxLength);
            line.Property(l => l.ScanSequence);
        });

        builder.OwnsMany(gr => gr.QuantityChecks, check =>
        {
            check.ToTable("gr_quantity_check");
            check.WithOwner().HasForeignKey("GoodsReceiptId");
            check.Property<int>("Id").ValueGeneratedOnAdd();
            check.HasKey("Id");
            check.Property(c => c.Sku).HasMaxLength(SkuMaxLength);
            check.Property(c => c.ExpectedQty).HasPrecision(18, 3);
            check.Property(c => c.ActualQty).HasPrecision(18, 3);
            check.Property(c => c.Variance).HasConversion<string>().HasMaxLength(EnumMaxLength);
        });

        builder.OwnsMany(gr => gr.Discrepancies, discrepancy =>
        {
            discrepancy.ToTable("gr_discrepancy");
            discrepancy.WithOwner().HasForeignKey("GoodsReceiptId");
            discrepancy.HasKey(d => d.Id);
            discrepancy.Property(d => d.Id).ValueGeneratedNever();
            discrepancy.Property(d => d.Sku).HasMaxLength(SkuMaxLength);
            discrepancy.Property(d => d.Type).HasConversion<string>().HasMaxLength(EnumMaxLength);
            discrepancy.Property(d => d.Qty).HasPrecision(18, 3);
        });

        builder.OwnsMany(gr => gr.Resolutions, resolution =>
        {
            resolution.ToTable("gr_resolution");
            resolution.WithOwner().HasForeignKey("GoodsReceiptId");

            // Satu resolution per discrepancy
            resolution.HasKey(r => r.DiscrepancyId);
            resolution.Property(r => r.DiscrepancyId).ValueGeneratedNever();
            resolution.Property(r => r.Action).HasConversion<string>().HasMaxLength(EnumMaxLength);
            resolution.Property(r => r.Note).HasMaxLength(500);
        });

        builder.OwnsMany(gr => gr.ReceivedLines, line =>
        {
            line.ToTable("gr_received_line");
            line.WithOwner().HasForeignKey("GoodsReceiptId");
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.Property(l => l.Sku).HasMaxLength(SkuMaxLength);
            line.Property(l => l.Qty).HasPrecision(18, 3);
            line.Property(l => l.Batch).HasMaxLength(64);
            line.Property(l => l.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);
        });

        builder.OwnsMany(gr => gr.RejectedLines, line =>
        {
            line.ToTable("gr_rejected_line");
            line.WithOwner().HasForeignKey("GoodsReceiptId");
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.HasKey("Id");
            line.Property(l => l.Sku).HasMaxLength(SkuMaxLength);
            line.Property(l => l.Qty).HasPrecision(18, 3);
            line.Property(l => l.Reason).HasConversion<string>().HasMaxLength(EnumMaxLength);
        });
    }
}
