using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain.UnitTests.TestData;

// Baseline balance valid
internal static class StockMother
{
    public const string SkuCode = "SKU-MILK";

    public static readonly Guid SourceGrId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid WarehouseId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    public static readonly Guid WaveId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid PickingTaskId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly Guid AssignedTo = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public static Sku MilkSku => Sku.Create(SkuCode).Value;

    public static LocationId ReceivingLocation => LocationId.Create(Guid.Parse("a0000000-0000-0000-0000-000000000001")).Value;

    public static LocationId RackLocation => LocationId.Create(Guid.Parse("a0000000-0000-0000-0000-000000000002")).Value;

    public static LocationId StagingLocation => LocationId.Create(Guid.Parse("a0000000-0000-0000-0000-000000000003")).Value;

    public static LocationId QuarantineLocation => LocationId.Create(Guid.Parse("a0000000-0000-0000-0000-000000000004")).Value;

    public static StockId NewStockId() => StockId.Create(Guid.NewGuid()).Value;

    public static StockReservationId NewReservationId() => StockReservationId.Create(Guid.NewGuid()).Value;

    public static PutawayTaskId NewPutawayTaskId() => PutawayTaskId.Create(Guid.NewGuid()).Value;

    public static Batch BatchOf(string code = "LOT-01") => Batch.Create(code).Value;

    public static Expiry ExpiryOf(int year = 2026, int month = 12, int day = 31) => Expiry.Create(new DateOnly(year, month, day)).Value;

    public static Quantity QtyOf(decimal qty) => Quantity.Create(qty).Value;

    // Balance OnHand baru di receiving area (belum putaway).
    public static Stock OnHand(decimal qty = 100m, int line = 0)
        => Stock.CreateOnHand(NewStockId(), MilkSku, ReceivingLocation, BatchOf(), ExpiryOf(), QtyOf(qty), SourceGrId, line, WarehouseId).Value;

    // Balance Quarantine (line QcHold).
    public static Stock Quarantine(decimal qty = 100m, int line = 0)
        => Stock.CreateQuarantine(NewStockId(), MilkSku, QuarantineLocation, BatchOf(), ExpiryOf(), QtyOf(qty), SourceGrId, line, WarehouseId).Value;

    // Balance Available (sudah putaway ke rak) — allocatable.
    public static Stock Available(decimal qty = 100m)
    {
        var stock = OnHand(qty);
        stock.PutAway(RackLocation);
        return stock;
    }

    // Balance Available dengan batch/expiry tertentu — untuk test FEFO.
    public static Stock AvailableWith(DateOnly expiry, string batch = "LOT-01", decimal qty = 100m)
    {
        var stock = Stock.CreateOnHand(
            NewStockId(), MilkSku, ReceivingLocation, Batch.Create(batch).Value, Expiry.Create(expiry).Value, QtyOf(qty), SourceGrId, 0, WarehouseId).Value;
        stock.PutAway(RackLocation);
        return stock;
    }

    // Balance Picked
    public static Stock Picked(decimal qty = 60m)
    {
        var source = Available(qty);
        var reservationId = NewReservationId();
        source.Reserve(reservationId, WaveId, OrderId, QtyOf(qty));
        return source.Pick(reservationId, NewStockId(), PickingTaskId, StagingLocation).Value;
    }

    // Reservasi Active terikat wave (aggregate root tersendiri).
    public static StockReservation ActiveReservation(decimal qty = 60m)
        => StockReservation.Create(NewReservationId(), NewStockId(), WaveId, OrderId, MilkSku, BatchOf(), QtyOf(qty)).Value;

    // Tugas putaway Assigned untuk Stock OnHand.
    public static PutawayTask AssignedPutaway()
        => PutawayTask.Create(NewPutawayTaskId(), NewStockId(), ReceivingLocation, RackLocation, AssignedTo).Value;
}
