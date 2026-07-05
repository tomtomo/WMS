using Wms.Outbound.Domain.ValueObjects;

namespace Wms.Outbound.Domain.UnitTests.TestData;

// Baseline order valid
internal static class OutboundOrderMother
{
    public const string Sku = "SKU-MILK";

    public const string UomCode = "CARTON";

    public static readonly Guid CustomerId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    public static Uom Carton => Uom.Create(UomCode).Value;

    public static ShipTo DefaultShipTo => ShipTo.Create("Toko Tom", "Jl. Merdeka 1", "Jakarta").Value;

    public static OutboundOrderId NewOrderId() => OutboundOrderId.Create(Guid.NewGuid()).Value;

    public static WaveId NewWaveId() => WaveId.Create(Guid.NewGuid()).Value;

    public static OrderLine LineOf(string sku = Sku, decimal qty = 10m) => OrderLine.Create(sku, qty, Carton).Value;

    // Order New single line.
    public static OutboundOrder New(decimal qty = 10m)
        => OutboundOrder.Create(NewOrderId(), CustomerId, DefaultShipTo, [LineOf(Sku, qty)]).Value;

    // Order InProgress (sudah diwave), single line.
    public static OutboundOrder InProgress(decimal qty = 10m)
    {
        var order = New(qty);
        order.AssignToWave(NewWaveId());
        return order;
    }

    // Order InProgress dengan line eksplisit (multi-SKU).
    public static OutboundOrder InProgressWith(params OrderLine[] lines)
    {
        var order = OutboundOrder.Create(NewOrderId(), CustomerId, DefaultShipTo, lines).Value;
        order.AssignToWave(NewWaveId());
        return order;
    }

    // Order teralokasi penuh (semua line Allocated) — siap Close.
    public static OutboundOrder FullyAllocated(decimal qty = 10m)
    {
        var order = InProgress(qty);
        order.ApplyAllocation([new AllocationLine(Sku, Guid.NewGuid(), qty)], []);
        return order;
    }

    // Order teralokasi parsial (allocated < qty) — punya backorder.
    public static OutboundOrder PartiallyAllocated(decimal qty = 10m, decimal allocated = 8m)
    {
        var order = InProgress(qty);
        order.ApplyAllocation(
            [new AllocationLine(Sku, Guid.NewGuid(), allocated)],
            [new Shortfall(Sku, qty, allocated, qty - allocated)]);
        return order;
    }
}
