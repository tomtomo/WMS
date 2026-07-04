using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;

// SPV membuat header GR.
[RequiresPermission(InboundPermissions.CreateGR)]
public sealed record CreateGoodsReceiptHeaderCommand(
    string PoRef,
    Guid SupplierId,
    Guid WarehouseId,
    string DockDoor,
    IReadOnlyList<ExpectedLineInput> ExpectedLines) : ICommand<Guid>;

public sealed record ExpectedLineInput(string Sku, decimal ExpectedQty, string Uom);
