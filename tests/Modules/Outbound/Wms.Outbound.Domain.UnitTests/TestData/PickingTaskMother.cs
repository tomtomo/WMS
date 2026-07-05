using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Outbound.Domain.UnitTests.TestData;

// Baseline PickingTask valid.
internal static class PickingTaskMother
{
    public const string Sku = "SKU-MILK";

    public const string Batch = "LOT-01";

    public static readonly Guid ReservationId = Guid.Parse("d0000000-0000-0000-0000-000000000001");

    public static readonly Guid StockId = Guid.Parse("d0000000-0000-0000-0000-000000000002");

    public static readonly Guid SourceLocation = Guid.Parse("d0000000-0000-0000-0000-000000000003");

    public static readonly Guid StagingLocation = Guid.Parse("d0000000-0000-0000-0000-000000000004");

    public static readonly Guid Operator = Guid.Parse("d0000000-0000-0000-0000-000000000005");

    public static PickingTaskId NewTaskId() => PickingTaskId.Create(Guid.NewGuid()).Value;

    public static WaveId NewWaveId() => WaveId.Create(Guid.NewGuid()).Value;

    // Task Assigned untuk satu entry allocations[].
    public static PickingTask Assigned(WaveId? waveId = null, Guid? reservationId = null, decimal qty = 10m)
        => PickingTask.Create(
            NewTaskId(),
            waveId ?? NewWaveId(),
            reservationId ?? ReservationId,
            StockId,
            SourceLocation,
            Sku,
            Batch,
            qty,
            Operator).Value;

    // Task Completed (picking selesai, actualQty == qty).
    public static PickingTask Completed(WaveId? waveId = null, Guid? reservationId = null, decimal qty = 10m)
    {
        var task = Assigned(waveId, reservationId, qty);
        task.Complete(qty, StagingLocation);
        return task;
    }

    // Create dengan override satu argumen
    public static Result<PickingTask> TryCreate(
        Guid? reservationId = null,
        Guid? stockId = null,
        string sku = Sku,
        decimal qty = 10m,
        Guid? assignedTo = null)
        => PickingTask.Create(
            NewTaskId(),
            NewWaveId(),
            reservationId ?? ReservationId,
            stockId ?? StockId,
            SourceLocation,
            sku,
            Batch,
            qty,
            assignedTo ?? Operator);
}
