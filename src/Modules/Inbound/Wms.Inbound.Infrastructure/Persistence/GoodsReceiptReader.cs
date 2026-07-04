using Microsoft.EntityFrameworkCore;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.ReadModels;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Infrastructure.Persistence;

// Read port detail/review — AsNoTracking, map ke DTO tanpa AutoMapper.
internal sealed class GoodsReceiptReader(InboundDbContext context) : IGoodsReceiptReader
{
    public Task<bool> ExistsAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default)
    {
        var id = GoodsReceiptId.Create(goodsReceiptId);
        return id.IsFailure
            ? Task.FromResult(false)
            : context.Set<GoodsReceipt>().AsNoTracking().AnyAsync(gr => gr.Id == id.Value, cancellationToken);
    }

    public async Task<GoodsReceiptDto?> GetDetailAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default)
    {
        var goodsReceipt = await LoadAsync(goodsReceiptId, cancellationToken);
        return goodsReceipt is null ? null : MapDetail(goodsReceipt);
    }

    public async Task<GoodsReceiptReviewDto?> GetReviewAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default)
    {
        var goodsReceipt = await LoadAsync(goodsReceiptId, cancellationToken);
        return goodsReceipt is null ? null : MapReview(goodsReceipt);
    }

    private static GoodsReceiptDto MapDetail(GoodsReceipt gr) => new(
        gr.Id.Value,
        gr.PoRef,
        gr.SupplierId,
        gr.WarehouseId,
        gr.DockDoor.Value,
        gr.Status.ToString(),
        gr.HoldReason?.Value,
        [.. gr.ExpectedLines.Select(line => new ExpectedLineDto(line.Sku, line.ExpectedQty, line.Uom))],
        [.. gr.ScannedLines
            .OrderBy(line => line.ScanSequence)
            .Select(line => new ScannedLineDto(line.Sku, line.ActualQty, line.Batch, line.Expiry, line.LineStatus.ToString()))],
        [.. gr.QuantityChecks.Select(check => new QuantityCheckDto(check.Sku, check.ExpectedQty, check.ActualQty, check.Variance.ToString()))],
        [.. gr.Discrepancies.Select(discrepancy => new DiscrepancyDto(discrepancy.Id, discrepancy.Sku, discrepancy.Type.ToString(), discrepancy.Qty))],
        [.. gr.Resolutions.Select(resolution => new ResolutionDto(resolution.DiscrepancyId, resolution.Action.ToString(), resolution.Note))],
        [.. gr.ReceivedLines.Select(line => new ReceivedLineDto(line.Sku, line.Qty, line.Batch, line.Expiry, line.Status.ToString()))],
        [.. gr.RejectedLines.Select(line => new RejectedLineDto(line.Sku, line.Qty, line.Reason.ToString()))]);

    private static GoodsReceiptReviewDto MapReview(GoodsReceipt gr)
    {
        var uomBySku = gr.ExpectedLines.ToDictionary(line => line.Sku, line => line.Uom, StringComparer.Ordinal);
        var resolutionByDiscrepancy = gr.Resolutions.ToDictionary(resolution => resolution.DiscrepancyId);

        var lines = gr.QuantityChecks
            .Select(check => new ReviewLineDto(
                check.Sku,
                uomBySku.TryGetValue(check.Sku, out var uom) ? uom : string.Empty,
                check.ExpectedQty,
                check.ActualQty,
                check.Variance.ToString()))
            .ToList();

        var groups = gr.Discrepancies
            .GroupBy(discrepancy => (discrepancy.Sku, discrepancy.Type))
            .Select(group => new DiscrepancyGroupDto(
                group.Key.Sku,
                group.Key.Type.ToString(),
                [.. group.Select(discrepancy => ToItem(discrepancy, resolutionByDiscrepancy))]))
            .ToList();

        var unresolvedCount = gr.Discrepancies.Count(discrepancy => !resolutionByDiscrepancy.ContainsKey(discrepancy.Id));
        var hasOverDelivery = gr.QuantityChecks.Any(check => check.Variance == Domain.Enums.QuantityVariance.OverDelivery);

        return new GoodsReceiptReviewDto(gr.Id.Value, gr.Status.ToString(), hasOverDelivery, unresolvedCount, lines, groups);
    }

    private static DiscrepancyItemDto ToItem(Discrepancy discrepancy, IReadOnlyDictionary<Guid, Resolution> resolutions)
    {
        var resolved = resolutions.TryGetValue(discrepancy.Id, out var resolution);
        return new DiscrepancyItemDto(
            discrepancy.Id,
            discrepancy.Qty,
            resolved,
            resolved ? resolution!.Action.ToString() : null,
            resolved ? resolution!.Note : null);
    }

    private async Task<GoodsReceipt?> LoadAsync(Guid goodsReceiptId, CancellationToken cancellationToken)
    {
        var id = GoodsReceiptId.Create(goodsReceiptId);
        if (id.IsFailure)
        {
            return null;
        }

        return await context.Set<GoodsReceipt>()
            .AsNoTracking()
            .FirstOrDefaultAsync(gr => gr.Id == id.Value, cancellationToken);
    }
}
