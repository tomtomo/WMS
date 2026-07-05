using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.Events;

namespace Wms.Outbound.Domain;

// Instruksi ambil stock dari satu lokasi rak ke staging. Satu task per entry allocations[].
public sealed class PickingTask : AggregateRoot<PickingTaskId>, IAuditable
{
    private PickingTask(
        PickingTaskId id,
        WaveId waveId,
        Guid reservationId,
        Guid stockId,
        Guid sourceLocationId,
        string sku,
        string? batch,
        decimal qty,
        Guid assignedTo)
        : base(id)
    {
        WaveId = waveId;
        ReservationId = reservationId;
        StockId = stockId;
        SourceLocationId = sourceLocationId;
        Sku = sku;
        Batch = batch;
        Qty = qty;
        AssignedTo = assignedTo;
        Status = PickingTaskStatus.Assigned;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private PickingTask()
        : base(default!)
    {
        WaveId = null!;
        Sku = string.Empty;
    }

    public WaveId WaveId { get; }

    public Guid ReservationId { get; }

    public Guid StockId { get; }

    public Guid SourceLocationId { get; }

    public string Sku { get; }

    public string? Batch { get; }

    public decimal Qty { get; }

    public Guid AssignedTo { get; }

    public PickingTaskStatus Status { get; private set; }

    public decimal? ActualQty { get; private set; }

    public Guid? StagingLocationId { get; private set; }

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // natural key (waveId, reservationId)
    public static Result<PickingTask> Create(
        PickingTaskId id,
        WaveId waveId,
        Guid reservationId,
        Guid stockId,
        Guid sourceLocationId,
        string sku,
        string? batch,
        decimal qty,
        Guid assignedTo)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(waveId);

        if (reservationId == Guid.Empty)
        {
            return Result.Invalid<PickingTask>(new Error("picking_task.reservation_required", "ReservationId wajib diisi."));
        }

        if (stockId == Guid.Empty)
        {
            return Result.Invalid<PickingTask>(new Error("picking_task.stock_required", "StockId wajib diisi."));
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Invalid<PickingTask>(new Error("picking_task.sku_required", "SKU wajib diisi."));
        }

        if (qty <= 0)
        {
            return Result.Invalid<PickingTask>(new Error("picking_task.qty_invalid", "Qty harus lebih dari nol."));
        }

        if (assignedTo == Guid.Empty)
        {
            return Result.Invalid<PickingTask>(new Error("picking_task.operator_required", "AssignedTo wajib diisi."));
        }

        var normalizedBatch = string.IsNullOrWhiteSpace(batch) ? null : batch.Trim();
        var task = new PickingTask(id, waveId, reservationId, stockId, sourceLocationId, sku.Trim(), normalizedBatch, qty, assignedTo);
        task.Raise(new PickingTaskAssignedRaised(id, waveId, stockId, reservationId, sku.Trim(), assignedTo));
        return Result.Success(task);
    }

    // picking selesai, stock pindah ke staging (Picked). Scope sekarang asumsi actualQty == qty.
    public Result Complete(decimal actualQty, Guid stagingLocationId)
    {
        if (Status != PickingTaskStatus.Assigned)
        {
            return Result.Conflict(new Error("picking_task.not_assigned", "Complete hanya bisa saat task Assigned."));
        }

        if (stagingLocationId == Guid.Empty)
        {
            return Result.Invalid(new Error("picking_task.staging_required", "StagingLocationId wajib diisi."));
        }

        // Picking discrepancy (actualQty < qty) belum di scope
        if (actualQty != Qty)
        {
            return Result.Invalid(new Error("picking_task.qty_mismatch", "actualQty harus sama dengan qty"));
        }

        ActualQty = actualQty;
        StagingLocationId = stagingLocationId;
        Status = PickingTaskStatus.Completed;
        return Result.Success();
    }
}
