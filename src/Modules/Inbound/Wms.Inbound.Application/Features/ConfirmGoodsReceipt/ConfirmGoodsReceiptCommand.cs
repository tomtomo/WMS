using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inbound.Application.Features.ConfirmGoodsReceipt;

// SPV post GR. Semua discrepancy wajib resolved, hasilnya receivedLines/rejectedLines dan GRConfirmed.
[RequiresPermission(InboundPermissions.PostGR)]
public sealed record ConfirmGoodsReceiptCommand(Guid GoodsReceiptId) : ICommand;
