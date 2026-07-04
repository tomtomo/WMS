using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Application.Features.ResolveDiscrepancy;

// keputusan SPV per discrepancy
[RequiresPermission(InboundPermissions.ResolveGR)]
public sealed record ResolveDiscrepancyCommand(
    Guid GoodsReceiptId,
    Guid DiscrepancyId,
    ResolutionAction Action,
    string? Note) : ICommand;
