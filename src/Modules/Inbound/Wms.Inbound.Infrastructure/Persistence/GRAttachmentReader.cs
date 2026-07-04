using Microsoft.EntityFrameworkCore;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.ReadModels;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Infrastructure.Persistence;

internal sealed class GRAttachmentReader(InboundDbContext context) : IGRAttachmentReader
{
    public async Task<IReadOnlyList<GRAttachmentDto>> ListByGoodsReceiptAsync(
        Guid goodsReceiptId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var id = GoodsReceiptId.Create(goodsReceiptId);
        if (id.IsFailure)
        {
            return [];
        }

        IQueryable<GRAttachment> query = context.Set<GRAttachment>().AsNoTracking();
        if (includeInactive)
        {
            // Bypass soft delete filter — untuk kebutuhan audit.
            query = query.IgnoreQueryFilters();
        }

        var attachments = await query
            .Where(attachment => attachment.GoodsReceiptId == id.Value)
            .OrderBy(attachment => attachment.UploadedAt)
            .ToListAsync(cancellationToken);

        return [.. attachments.Select(attachment => new GRAttachmentDto(
            attachment.Id.Value,
            attachment.GoodsReceiptId.Value,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedAt,
            attachment.IsActive))];
    }

    public async Task<string?> GetActiveContentRefAsync(
        Guid goodsReceiptId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var grId = GoodsReceiptId.Create(goodsReceiptId);
        var id = GRAttachmentId.Create(attachmentId);
        if (grId.IsFailure || id.IsFailure)
        {
            return null;
        }

        var contentRef = await context.Set<GRAttachment>()
            .AsNoTracking()
            .Where(attachment => attachment.Id == id.Value && attachment.GoodsReceiptId == grId.Value)
            .Select(attachment => attachment.ContentRef)
            .FirstOrDefaultAsync(cancellationToken);

        return contentRef?.Value;
    }
}
