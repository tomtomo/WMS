using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Application.Features.ScanReceiptLine;

// Satu entry scan operator. SKU di luar expected wajib di tag WrongItem.
[RequiresPermission(InboundPermissions.ScanGR)]
public sealed record ScanReceiptLineCommand(
    Guid GoodsReceiptId,
    string Sku,
    decimal ActualQty,
    string? Batch,
    DateOnly? Expiry,
    LineStatus LineStatus) : ICommand;
