using Wms.Outbound.Domain.ValueObjects;

namespace Wms.Outbound.Domain.UnitTests.TestData;

// Baseline wave valid.
internal static class WaveMother
{
    public static readonly Guid WarehouseId = Guid.Parse("11111111-1111-1111-1111-000000000001");

    public static CancelReason AnyReason => CancelReason.Create("wave nol-terpenuhi").Value;

    public static WaveId NewWaveId() => WaveId.Create(Guid.NewGuid()).Value;

    public static OutboundOrderId NewOrderId() => OutboundOrderId.Create(Guid.NewGuid()).Value;

    // Wave Active dengan 1 order, reservationIds opsional.
    public static Wave Active(params Guid[] reservationIds)
        => Wave.Create(NewWaveId(), WarehouseId, [NewOrderId()], reservationIds).Value;
}
