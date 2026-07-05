using Microsoft.EntityFrameworkCore;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.ReadModels;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Enums;

namespace Wms.Outbound.Infrastructure.Persistence;

// Read port OutboundOrder — AsNoTracking, map ke DTO.
internal sealed class OutboundOrderReader(OutboundDbContext context) : IOutboundOrderReader
{
    public async Task<IReadOnlyList<OutboundOrderDto>> GetBacklogAsync(CancellationToken cancellationToken = default)
    {
        var orders = await context.Set<OutboundOrder>().AsNoTracking()
            .Where(order => order.Status == OutboundOrderStatus.New)
            .ToListAsync(cancellationToken);
        return [.. orders.Select(Map)];
    }

    public async Task<OutboundOrderDto?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var id = OutboundOrderId.Create(orderId);
        if (id.IsFailure)
        {
            return null;
        }

        var order = await context.Set<OutboundOrder>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        return order is null ? null : Map(order);
    }

    private static OutboundOrderDto Map(OutboundOrder order) => new(
        order.Id.Value,
        order.CustomerId,
        order.Status.ToString(),
        order.WaveId?.Value,
        [.. order.OrderLines.Select(line => new OutboundOrderLineDto(
            line.Sku, line.Qty, line.AllocatedQty, line.AllocationStatus.ToString()))]);
}
