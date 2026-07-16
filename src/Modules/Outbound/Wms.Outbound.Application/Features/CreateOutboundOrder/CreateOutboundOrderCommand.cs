using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Outbound.Application.Features.CreateOutboundOrder;

// Command untuk membuat OutboundOrder berstatus New dalam satu warehouse.
[RequiresPermission(OutboundPermissions.CreateOrder)]
public sealed record CreateOutboundOrderCommand(
    Guid CustomerId,
    string Recipient,
    string AddressLine,
    string City,
    IReadOnlyList<CreateOutboundOrderLine> Lines) : ICommand<Guid>;

public sealed record CreateOutboundOrderLine(string Sku, decimal Qty, string Uom);
