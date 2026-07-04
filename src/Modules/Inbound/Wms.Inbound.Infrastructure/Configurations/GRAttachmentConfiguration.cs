using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Infrastructure.Configurations;

// Mapping GRAttachment
public sealed class GRAttachmentConfiguration : IEntityTypeConfiguration<GRAttachment>
{
    public void Configure(EntityTypeBuilder<GRAttachment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("gr_attachment");

        builder.HasKey(attachment => attachment.Id);
        builder.Property(attachment => attachment.Id)
            .HasConversion(id => id.Value, value => GRAttachmentId.Create(value).Value)
            .ValueGeneratedNever();

        builder.Property(attachment => attachment.GoodsReceiptId)
            .HasConversion(id => id.Value, value => GoodsReceiptId.Create(value).Value);
        builder.HasIndex(attachment => attachment.GoodsReceiptId);

        builder.Property(attachment => attachment.FileName).HasMaxLength(GRAttachment.MaxFileNameLength);
        builder.Property(attachment => attachment.ContentType).HasMaxLength(128);
        builder.Property(attachment => attachment.ContentRef)
            .HasConversion(contentRef => contentRef.Value, value => ContentRef.Create(value).Value)
            .HasMaxLength(512);
        builder.Property(attachment => attachment.SizeBytes);

        // Get only tanpa setter tidak di map EF
        builder.Property(attachment => attachment.UploadedAt);
        builder.Property(attachment => attachment.IsActive);

        builder.Property(attachment => attachment.CreatedBy).HasMaxLength(200);
        builder.Property(attachment => attachment.ModifiedBy).HasMaxLength(200);

        // Soft deleted tidak terlihat query normal
        builder.HasQueryFilter(attachment => attachment.IsActive);

        builder.UseXminConcurrencyToken();
    }
}
