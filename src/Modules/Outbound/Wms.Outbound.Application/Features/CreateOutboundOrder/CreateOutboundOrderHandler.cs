using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.ValueObjects;

namespace Wms.Outbound.Application.Features.CreateOutboundOrder;

// Ubah request menjadi value object domain, lalu buat order berstatus New dengan setiap line berstatus Pending. Order disimpan ke backlog sampai masuk wave.
internal sealed class CreateOutboundOrderHandler(IOutboundOrderRepository orderRepository)
    : ICommandHandler<CreateOutboundOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOutboundOrderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shipTo = ShipTo.Create(command.Recipient, command.AddressLine, command.City);
        if (shipTo.IsFailure)
        {
            return shipTo.ForwardFailure<Guid>();
        }

        var lines = new List<OrderLine>(command.Lines.Count);
        foreach (var input in command.Lines)
        {
            var uom = Uom.Create(input.Uom);
            if (uom.IsFailure)
            {
                return uom.ForwardFailure<Guid>();
            }

            var line = OrderLine.Create(input.Sku, input.Qty, uom.Value);
            if (line.IsFailure)
            {
                return line.ForwardFailure<Guid>();
            }

            lines.Add(line.Value);
        }

        var orderId = OutboundOrderId.Create(Guid.NewGuid()).Value;
        var order = OutboundOrder.Create(orderId, command.CustomerId, shipTo.Value, lines);
        if (order.IsFailure)
        {
            return order.ForwardFailure<Guid>();
        }

        await orderRepository.AddAsync(order.Value, cancellationToken);
        return Result.Success(orderId.Value);
    }
}
