using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.Features.CompleteScan;
using Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;
using Wms.Inbound.Application.Features.ResolveDiscrepancy;
using Wms.Inbound.Application.Features.ScanReceiptLine;
using Wms.Inbound.Application.ReadModels;
using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.IntegrationTests.TestSupport;

internal static class GoodsReceiptScenarios
{
    public static readonly Guid SupplierId = Guid.Parse("7d9f8a10-1111-4222-8333-944445555666");

    public static readonly Guid WarehouseId = Guid.Parse("2b1c3d40-aaaa-4bbb-8ccc-9ddddeeeefff");

    public static async Task<Guid> CreateAsync(
        IServiceProvider provider,
        params (string Sku, decimal Qty)[] expectedLines)
    {
        var command = new CreateGoodsReceiptHeaderCommand(
            "PO-2026-001",
            SupplierId,
            WarehouseId,
            "DOCK-1",
            [.. expectedLines.Select(line => new ExpectedLineInput(line.Sku, line.Qty, "EA"))]);

        var created = await PipelineRunner.SendAsync(provider, command);
        created.IsSuccess.Should().BeTrue($"create header harus sukses: {created.Error.Message}");
        return created.Value;
    }

    public static async Task ScanAsync(
        IServiceProvider provider,
        Guid goodsReceiptId,
        string sku,
        decimal qty,
        LineStatus status = LineStatus.Good,
        string? batch = null,
        DateOnly? expiry = null)
    {
        var scanned = await PipelineRunner.SendAsync(
            provider,
            new ScanReceiptLineCommand(goodsReceiptId, sku, qty, batch, expiry, status));
        scanned.IsSuccess.Should().BeTrue($"scan {sku} harus sukses: {scanned.Error.Message}");
    }

    public static async Task CompleteScanAsync(IServiceProvider provider, Guid goodsReceiptId)
    {
        var completed = await PipelineRunner.SendAsync(provider, new CompleteScanCommand(goodsReceiptId));
        completed.IsSuccess.Should().BeTrue($"complete scan harus sukses: {completed.Error.Message}");
    }

    // Resolve semua discrepancy dengan action default
    public static async Task ResolveAllAsync(IServiceProvider provider, Guid goodsReceiptId)
    {
        foreach (var discrepancy in await ListDiscrepanciesAsync(provider, goodsReceiptId))
        {
            var action = discrepancy.Type switch
            {
                nameof(DiscrepancyType.ShortDelivery) => ResolutionAction.AcceptPartial,
                nameof(DiscrepancyType.OverDelivery) => ResolutionAction.RejectExcess,
                nameof(DiscrepancyType.WrongItem) => ResolutionAction.ReturnToSupplier,
                _ => ResolutionAction.SendToQC,
            };

            var resolved = await PipelineRunner.SendAsync(
                provider,
                new ResolveDiscrepancyCommand(goodsReceiptId, discrepancy.DiscrepancyId, action, "auto-resolve test"));
            resolved.IsSuccess.Should().BeTrue($"resolve {discrepancy.Type} harus sukses: {resolved.Error.Message}");
        }
    }

    public static async Task<GoodsReceiptDto> ReadDetailAsync(IServiceProvider provider, Guid goodsReceiptId)
    {
        using var scope = provider.CreateScope();
        var detail = await scope.ServiceProvider.GetRequiredService<IGoodsReceiptReader>()
            .GetDetailAsync(goodsReceiptId);
        detail.Should().NotBeNull();
        return detail!;
    }

    private static async Task<IReadOnlyList<DiscrepancyDto>> ListDiscrepanciesAsync(
        IServiceProvider provider,
        Guid goodsReceiptId)
    {
        var detail = await ReadDetailAsync(provider, goodsReceiptId);
        return detail.Discrepancies;
    }
}
