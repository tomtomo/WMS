using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.Events;
using Wms.Outbound.Domain.ValueObjects;

namespace Wms.Outbound.Domain;

// Grouping OutboundOrder yang diproses dan di dispatch bersama.
public sealed class Wave : AggregateRoot<WaveId>, IAuditable
{
    private readonly List<OutboundOrderId> _orderIds;

    // Referensi ke reservasi milik Inventory, bukan salinan detail alokasi.
    private readonly List<Guid> _reservationIds;

    private readonly List<PickingTaskId> _pickingTaskIds = [];

    private Wave(WaveId id, Guid warehouseId, List<OutboundOrderId> orderIds, List<Guid> reservationIds)
        : base(id)
    {
        WarehouseId = warehouseId;
        _orderIds = orderIds;
        _reservationIds = reservationIds;
        Status = WaveStatus.Active;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private Wave()
        : base(default!)
    {
        _orderIds = [];
        _reservationIds = [];
    }

    public WaveStatus Status { get; private set; }

    // Warehouse tempat wave dieksekusi — dibawa ke payload PickingTaskAssigned/WaveReady.
    public Guid WarehouseId { get; }

    public CancelReason? CancelReason { get; private set; }

    public IReadOnlyList<OutboundOrderId> OrderIds => _orderIds.AsReadOnly();

    public IReadOnlyList<Guid> ReservationIds => _reservationIds.AsReadOnly();

    public IReadOnlyList<PickingTaskId> PickingTaskIds => _pickingTaskIds.AsReadOnly();

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // SPV membuat wave dari beberapa order di satu warehouse. reservationIds diisi saat reservasi Inventory diketahui.
    public static Result<Wave> Create(
        WaveId id,
        Guid warehouseId,
        IEnumerable<OutboundOrderId> orderIds,
        IEnumerable<Guid> reservationIds)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(orderIds);
        ArgumentNullException.ThrowIfNull(reservationIds);

        if (warehouseId == Guid.Empty)
        {
            return Result.Invalid<Wave>(new Error("wave.warehouse_required", "WarehouseId wajib diisi."));
        }

        var orders = orderIds.ToList();
        if (orders.Count == 0)
        {
            return Result.Invalid<Wave>(new Error("wave.orders_required", "Wave harus punya minimal satu order."));
        }

        return Result.Success(new Wave(id, warehouseId, orders, reservationIds.ToList()));
    }

    // daftarkan PickingTask ke wave. Idempotent
    public Result AttachPickingTask(PickingTaskId taskId)
    {
        ArgumentNullException.ThrowIfNull(taskId);

        if (Status != WaveStatus.Active)
        {
            return Result.Conflict(new Error("wave.not_active", "AttachPickingTask hanya bisa saat wave Active."));
        }

        if (!_pickingTaskIds.Contains(taskId))
        {
            _pickingTaskIds.Add(taskId);
        }

        return Result.Success();
    }

    // wave Ready saat semua PickingTask yang ada Completed. Line Short/Partial tak punya task
    public Result EvaluateReadiness(IEnumerable<PickingTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        if (Status != WaveStatus.Active)
        {
            return Result.Conflict(new Error("wave.not_active", "EvaluateReadiness hanya bisa saat wave Active."));
        }

        var completedIds = tasks
            .Where(task => task.Status == PickingTaskStatus.Completed)
            .Select(task => task.Id)
            .ToHashSet();

        var allDone = _pickingTaskIds.Count > 0 && _pickingTaskIds.TrueForAll(completedIds.Contains);
        if (allDone)
        {
            Status = WaveStatus.Ready;
            Raise(new WaveReadyRaised(Id));
        }

        return Result.Success();
    }

    // auto-cancel saat outcome Unfulfilled.
    public Result AutoCancel(CancelReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (Status != WaveStatus.Active)
        {
            return Result.Conflict(new Error("wave.not_active", "AutoCancel hanya bisa saat wave Active."));
        }

        CancelReason = reason;
        Status = WaveStatus.Cancelled;
        Raise(new WaveCancelledRaised(Id, reason.Value));
        return Result.Success();
    }

    // dispatch wave siap.
    public Result Dispatch()
    {
        if (Status != WaveStatus.Ready)
        {
            return Result.Conflict(new Error("wave.not_ready", "Dispatch hanya bisa saat wave Ready."));
        }

        Status = WaveStatus.Dispatched;
        Raise(new WaveDispatchedRaised(Id));
        return Result.Success();
    }
}
