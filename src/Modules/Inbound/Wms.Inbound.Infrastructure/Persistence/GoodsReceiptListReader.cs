using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.ReadModels;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Infrastructure.Persistence;

// Antrean review SPV, paged & bounded.
internal sealed class GoodsReceiptListReader(InboundDbContext context) : IGoodsReceiptListReader
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<GoodsReceiptListItemDto>> ListPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var pending = context.Set<GoodsReceipt>()
            .AsNoTracking()
            .Where(gr => gr.Status == GoodsReceiptStatus.Pending);

        var totalCount = await pending.CountAsync(cancellationToken);

        // FIFO: yang paling lama menunggu review tampil dulu.
        var rows = await pending
            .OrderBy(gr => gr.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(gr => new
            {
                gr.Id,
                gr.PoRef,
                gr.SupplierId,
                gr.WarehouseId,
                gr.DockDoor,
                gr.Status,
                DiscrepancyCount = gr.Discrepancies.Count,
                gr.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new GoodsReceiptListItemDto(
                row.Id.Value,
                row.PoRef,
                row.SupplierId,
                row.WarehouseId,
                row.DockDoor.Value,
                row.Status.ToString(),
                row.DiscrepancyCount,
                row.CreatedAt))
            .ToList();

        return new PagedResult<GoodsReceiptListItemDto>(items, totalCount, safePage, safePageSize);
    }
}
