using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain;

// Instruksi memindah satu balance Stock dari receiving ke rak.
public sealed class PutawayTask : AggregateRoot<PutawayTaskId>, IAuditable
{
    private PutawayTask(
        PutawayTaskId id,
        StockId stockId,
        LocationId sourceLocationId,
        LocationId suggestedDestinationId,
        Guid assignedTo)
        : base(id)
    {
        StockId = stockId;
        SourceLocationId = sourceLocationId;
        SuggestedDestinationId = suggestedDestinationId;
        AssignedTo = assignedTo;
        Status = PutawayStatus.Assigned;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private PutawayTask()
        : base(default!)
    {
        StockId = null!;
        SourceLocationId = null!;
        SuggestedDestinationId = null!;
    }

    public StockId StockId { get; }

    public LocationId SourceLocationId { get; }

    public LocationId SuggestedDestinationId { get; }

    public Guid AssignedTo { get; }

    public PutawayStatus Status { get; private set; }

    public LocationId? ActualDestinationId { get; private set; }

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<PutawayTask> Create(
        PutawayTaskId id,
        StockId stockId,
        LocationId sourceLocationId,
        LocationId suggestedDestinationId,
        Guid assignedTo)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(stockId);
        ArgumentNullException.ThrowIfNull(sourceLocationId);
        ArgumentNullException.ThrowIfNull(suggestedDestinationId);

        if (assignedTo == Guid.Empty)
        {
            return Result.Invalid<PutawayTask>(new Error("putaway_task.assignee_required", "AssignedTo wajib diisi."));
        }

        var task = new PutawayTask(id, stockId, sourceLocationId, suggestedDestinationId, assignedTo);
        task.Raise(new PutawayTaskAssigned(id, stockId, assignedTo, suggestedDestinationId));

        return Result.Success(task);
    }

    // Operator scan stock dan destination: task Completed, Stock pindah lokasi & OnHand ke Available.
    public Result Complete(LocationId actualDestinationId)
    {
        ArgumentNullException.ThrowIfNull(actualDestinationId);

        if (Status != PutawayStatus.Assigned)
        {
            return Result.Conflict(new Error("putaway_task.not_assigned", "Complete hanya bisa saat task Assigned."));
        }

        ActualDestinationId = actualDestinationId;
        Status = PutawayStatus.Completed;
        Raise(new PutawayTaskCompleted(Id, StockId, actualDestinationId));

        return Result.Success();
    }
}
