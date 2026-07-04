using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inbound.Application.Features.HoldGoodsReceipt;

// SPV tolak seluruh GR, tanpa integration event.
[RequiresPermission(InboundPermissions.HoldGR)]
public sealed record HoldGoodsReceiptCommand(Guid GoodsReceiptId, string Reason) : ICommand;
