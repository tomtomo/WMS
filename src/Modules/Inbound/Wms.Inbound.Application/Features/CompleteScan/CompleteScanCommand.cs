using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inbound.Application.Features.CompleteScan;

[RequiresPermission(InboundPermissions.CompleteScanGR)]
public sealed record CompleteScanCommand(Guid GoodsReceiptId) : ICommand;
