using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;

namespace Wms.Inventory.IntegrationTests.TestSupport;

// Builder GRConfirmed untuk test consumer receiving.
internal static class GrConfirmedFactory
{
    public static readonly Guid WarehouseId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public static readonly Guid SupplierId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public static GRConfirmed With(Guid grId, params ReceivedLine[] lines) =>
        new(grId, WarehouseId, SupplierId, lines, []);

    public static ReceivedLine Good(
        string sku = "SKU-MILK",
        decimal qty = 100m,
        string batch = "LOT-01",
        DateOnly? expiry = null) =>
        new(sku, qty, batch, expiry ?? new DateOnly(2026, 12, 31), ReceivedLineStatus.Good);

    public static ReceivedLine QcHold(
        string sku = "SKU-MILK",
        decimal qty = 20m,
        string batch = "LOT-02",
        DateOnly? expiry = null) =>
        new(sku, qty, batch, expiry ?? new DateOnly(2026, 6, 30), ReceivedLineStatus.QcHold);
}
