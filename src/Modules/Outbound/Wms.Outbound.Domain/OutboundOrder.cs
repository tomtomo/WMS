using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.Events;
using Wms.Outbound.Domain.ValueObjects;

namespace Wms.Outbound.Domain;

// Order pengiriman customer, multi-SKU. Demand yang di wave, dialokasi, lalu ditutup saat terpenuhi.
public sealed class OutboundOrder : AggregateRoot<OutboundOrderId>, IAuditable
{
    private readonly List<OrderLine> _orderLines;

    // Sisa demand per line Partial/Short — re waveable
    private readonly List<Backorder> _backorders = [];

    private OutboundOrder(OutboundOrderId id, Guid customerId, ShipTo shipTo, List<OrderLine> orderLines)
        : base(id)
    {
        CustomerId = customerId;
        ShipTo = shipTo;
        _orderLines = orderLines;
        Status = OutboundOrderStatus.New;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private OutboundOrder()
        : base(default!)
    {
        ShipTo = null!;
        _orderLines = [];
    }

    public Guid CustomerId { get; }

    public ShipTo ShipTo { get; }

    public OutboundOrderStatus Status { get; private set; }

    // Kosong saat backlog (New), terisi saat di wave aktif, di clear saat kembali ke backlog.
    public WaveId? WaveId { get; private set; }

    public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();

    public IReadOnlyList<Backorder> Backorders => _backorders.AsReadOnly();

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // Order masuk. tiap line Pending, allocatedQty 0, state New.
    public static Result<OutboundOrder> Create(
        OutboundOrderId id,
        Guid customerId,
        ShipTo shipTo,
        IEnumerable<OrderLine> orderLines)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(shipTo);
        ArgumentNullException.ThrowIfNull(orderLines);

        if (customerId == Guid.Empty)
        {
            return Result.Invalid<OutboundOrder>(new Error("outbound_order.customer_required", "CustomerId wajib diisi."));
        }

        var snapshot = orderLines.ToList();
        if (snapshot.Count == 0)
        {
            return Result.Invalid<OutboundOrder>(new Error("outbound_order.lines_required", "Order harus punya minimal satu line."));
        }

        // SKU unik: ApplyAllocation memetakan allocation/shortfall ke line by SKU
        if (snapshot.GroupBy(line => line.Sku, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            return Result.Invalid<OutboundOrder>(new Error("outbound_order.sku_duplicated", "SKU order tidak boleh duplikat."));
        }

        return Result.Success(new OutboundOrder(id, customerId, shipTo, snapshot));
    }

    // Order dimasukkan ke wave aktif.
    public Result AssignToWave(WaveId waveId)
    {
        ArgumentNullException.ThrowIfNull(waveId);

        if (Status != OutboundOrderStatus.New)
        {
            return Result.Conflict(new Error("outbound_order.not_new", "AssignToWave hanya bisa dari state New."));
        }

        WaveId = waveId;
        Status = OutboundOrderStatus.InProgress;
        return Result.Success();
    }

    // Terjemahkan outcome alokasi ke allocationStatus per line dan catat backorder. Recompute
    // dari nol supaya outcome yang sama menghasilkan state yang sama.
    public Result ApplyAllocation(IEnumerable<AllocationLine> allocations, IEnumerable<Shortfall> shortfalls)
    {
        ArgumentNullException.ThrowIfNull(allocations);
        ArgumentNullException.ThrowIfNull(shortfalls);

        if (Status != OutboundOrderStatus.InProgress)
        {
            return Result.Conflict(new Error("outbound_order.not_in_progress", "ApplyAllocation hanya bisa saat order InProgress."));
        }

        // FEFO Inventory bisa reservasi lintas batch, jumlahkan allocatedQty per SKU.
        var allocatedBySku = allocations
            .GroupBy(allocation => allocation.Sku, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(allocation => allocation.AllocatedQty), StringComparer.Ordinal);

        var shortSkus = shortfalls.Select(shortfall => shortfall.Sku).ToHashSet(StringComparer.Ordinal);

        // allocation/shortfall untuk SKU di luar order
        var knownSkus = _orderLines.Select(line => line.Sku).ToHashSet(StringComparer.Ordinal);
        if (allocatedBySku.Keys.Concat(shortSkus).Any(sku => !knownSkus.Contains(sku)))
        {
            return Result.Invalid(new Error("outbound_order.allocation_sku_unknown", "Alokasi/shortfall memuat SKU di luar order."));
        }

        // allocatedQty <= qty diperiksa, order tidak berubah bila dilanggar.
        foreach (var line in _orderLines)
        {
            if (allocatedBySku.TryGetValue(line.Sku, out var overCheck) && overCheck > line.Qty)
            {
                return Result.Invalid(new Error("outbound_order.over_allocate", "Alokasi melebihi qty demand line."));
            }
        }

        _backorders.Clear();
        for (var index = 0; index < _orderLines.Count; index++)
        {
            var line = _orderLines[index];
            var inAllocations = allocatedBySku.TryGetValue(line.Sku, out var allocated);
            if (!inAllocations && !shortSkus.Contains(line.Sku))
            {
                continue; // line tidak tersentuh outcome, tetap Pending
            }

            _orderLines[index] = line with
            {
                AllocatedQty = allocated,
                AllocationStatus = AllocationStatusFor(allocated, line.Qty),
            };

            var shortQty = line.Qty - allocated;
            if (shortQty > 0)
            {
                _backorders.Add(new Backorder(line.Sku, shortQty));
            }
        }

        return Result.Success();
    }

    // order kembali ke backlog. Sisa demand per line = qty - allocatedQty. Line
    // yang habis terpenuhi (sisa 0) terdrop otomatis lewat  qty>0 di OrderLine.Create.
    public Result ReturnToBacklog(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != OutboundOrderStatus.InProgress)
        {
            return Result.Conflict(new Error("outbound_order.not_in_progress", "ReturnToBacklog hanya bisa saat order InProgress."));
        }

        var outstanding = _orderLines
            .Select(line => OrderLine.Create(line.Sku, line.Qty - line.AllocatedQty, line.Uom))
            .Where(line => line.IsSuccess)
            .Select(line => line.Value)
            .ToList();

        _orderLines.Clear();
        _orderLines.AddRange(outstanding);
        _backorders.Clear();
        WaveId = null;
        Status = OutboundOrderStatus.New;
        Raise(new OrderReturnedToBacklogRaised(Id, reason));
        return Result.Success();
    }

    // tutup order. Hanya order terpenuhi(semua line Allocated) yang boleh Closed. Terminal.
    public Result Close()
    {
        if (Status != OutboundOrderStatus.InProgress)
        {
            return Result.Conflict(new Error("outbound_order.not_in_progress", "Close hanya bisa saat order InProgress."));
        }

        if (!_orderLines.TrueForAll(line => line.AllocationStatus == AllocationStatus.Allocated))
        {
            return Result.Invalid(new Error("outbound_order.not_fully_fulfilled", "Hanya order terpenuhi penuh yang boleh Closed."));
        }

        Status = OutboundOrderStatus.Closed;
        Raise(new OutboundOrderClosedRaised(Id));
        return Result.Success();
    }

    // Mapping presisi allocationStatus per line.
    private static AllocationStatus AllocationStatusFor(decimal allocated, decimal demand)
    {
        if (allocated >= demand)
        {
            return AllocationStatus.Allocated;
        }

        return allocated > 0 ? AllocationStatus.PartiallyAllocated : AllocationStatus.Short;
    }
}
