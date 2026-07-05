using Wms.Outbound.Contracts;
using Wms.Outbound.Contracts.Payloads;

namespace Wms.Inventory.IntegrationTests.TestSupport;

// Builder WaveReleased untuk test consumer alokasi.
internal static class WaveReleasedFactory
{
    public static WaveReleased With(Guid waveId, params WaveLine[] lines) => new(waveId, lines);

    public static WaveLine Line(Guid orderId, string sku = "SKU-MILK", decimal qty = 10m) => new(orderId, sku, qty);
}
