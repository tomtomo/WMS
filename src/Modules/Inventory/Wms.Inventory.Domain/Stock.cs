using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain;

// Balance fisik per (SKU, Location, Batch). State = lokasi/kualitas fisik
public sealed class Stock : AggregateRoot<StockId>, IAuditable
{
    // Klaim reservasi Active
    private readonly List<ReservationClaim> _reservations = [];

    private Stock(
        StockId id,
        Sku sku,
        LocationId locationId,
        Batch batch,
        Expiry expiry,
        decimal qty,
        StockStatus status,
        Guid sourceGrId)
        : base(id)
    {
        Sku = sku;
        LocationId = locationId;
        Batch = batch;
        Expiry = expiry;
        Qty = qty;
        Status = status;
        SourceGrId = sourceGrId;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private Stock()
        : base(default!)
    {
        Sku = null!;
        LocationId = null!;
        Batch = null!;
        Expiry = null!;
    }

    public Sku Sku { get; }

    // Berubah saat PutAway ke rak dan Pick (balance baru ke staging).
    public LocationId LocationId { get; private set; }

    public Batch Batch { get; }

    public Expiry Expiry { get; }

    public decimal Qty { get; private set; }

    public StockStatus Status { get; private set; }

    public Guid SourceGrId { get; }

    // Diisi hanya saat balance Picked
    public Guid? PickingTaskId { get; private set; }

    public Guid? WaveId { get; private set; }

    // Hanya Available yang allocatable
    public decimal AvailableQty => Status == StockStatus.Available ? Qty - _reservations.Sum(claim => claim.Qty) : 0m;

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // line Good ke OnHand di receiving area.
    public static Result<Stock> CreateOnHand(
        StockId id,
        Sku sku,
        LocationId locationId,
        Batch batch,
        Expiry expiry,
        Quantity qty,
        Guid sourceGrId)
        => Create(id, sku, locationId, batch, expiry, qty, sourceGrId, StockStatus.OnHand);

    // line QcHold ke Quarantine (tidak allocatable, tidak generate PutawayTask).
    public static Result<Stock> CreateQuarantine(
        StockId id,
        Sku sku,
        LocationId locationId,
        Batch batch,
        Expiry expiry,
        Quantity qty,
        Guid sourceGrId)
        => Create(id, sku, locationId, batch, expiry, qty, sourceGrId, StockStatus.Quarantine);

    // hanya OnHand ke Available, pindah ke rak.
    public Result PutAway(LocationId rackLocationId)
    {
        ArgumentNullException.ThrowIfNull(rackLocationId);

        if (Status != StockStatus.OnHand)
        {
            return Result.Conflict(new Error("stock.not_on_hand", "PutAway hanya bisa dari state OnHand."));
        }

        LocationId = rackLocationId;
        Status = StockStatus.Available;
        Raise(new StockPutAway(Id, rackLocationId));

        return Result.Success();
    }

    // Klaim qty terhadap balance Available.
    public Result Reserve(StockReservationId reservationId, Guid waveId, Guid orderId, Quantity qty)
    {
        ArgumentNullException.ThrowIfNull(reservationId);
        ArgumentNullException.ThrowIfNull(qty);

        if (Status != StockStatus.Available)
        {
            return Result.Conflict(new Error("stock.not_available", "Reserve hanya bisa pada balance Available."));
        }

        if (_reservations.Exists(claim => claim.ReservationId == reservationId))
        {
            return Result.Conflict(new Error("stock.reservation_exists", "Reservasi dengan id ini sudah ada pada balance."));
        }

        if (waveId == Guid.Empty)
        {
            return Result.Invalid(new Error("stock.wave_required", "WaveId wajib diisi."));
        }

        if (orderId == Guid.Empty)
        {
            return Result.Invalid(new Error("stock.order_required", "OrderId wajib diisi."));
        }

        if (qty.Value > AvailableQty)
        {
            return Result.Invalid(new Error("stock.over_allocate", "Reserve melebihi available-to-promise."));
        }

        _reservations.Add(new ReservationClaim(reservationId, waveId, orderId, qty.Value));
        Raise(new StockReserved(Id, reservationId, waveId, orderId, qty.Value));

        return Result.Success();
    }

    // Lepas klaim ke availableQty balik.
    public Result ReleaseReservation(StockReservationId reservationId)
    {
        ArgumentNullException.ThrowIfNull(reservationId);

        var claim = _reservations.Find(candidate => candidate.ReservationId == reservationId);
        if (claim is null)
        {
            return Result.NotFound(new Error("stock.reservation_not_found", "Reservasi tidak ditemukan pada balance."));
        }

        _reservations.Remove(claim);
        Raise(new ReservationReleased(Id, reservationId));

        return Result.Success();
    }

    // split fisik: reservasi terpenuhi keluar dari balance Available, terbentuk balance Picked di staging.
    public Result<Stock> Pick(
        StockReservationId reservationId,
        StockId pickedStockId,
        Guid pickingTaskId,
        LocationId stagingLocationId)
    {
        ArgumentNullException.ThrowIfNull(reservationId);
        ArgumentNullException.ThrowIfNull(pickedStockId);
        ArgumentNullException.ThrowIfNull(stagingLocationId);

        if (Status != StockStatus.Available)
        {
            return Result.Conflict<Stock>(new Error("stock.not_available", "Pick hanya bisa dari balance Available."));
        }

        var claim = _reservations.Find(candidate => candidate.ReservationId == reservationId);
        if (claim is null)
        {
            return Result.NotFound<Stock>(new Error("stock.reservation_not_found", "Reservasi tidak ditemukan pada balance."));
        }

        _reservations.Remove(claim);
        Qty -= claim.Qty;

        var picked = new Stock(pickedStockId, Sku, stagingLocationId, Batch, Expiry, claim.Qty, StockStatus.Picked, SourceGrId)
        {
            PickingTaskId = pickingTaskId,
            WaveId = claim.WaveId,
        };

        Raise(new StockPicked(Id, pickedStockId, reservationId, pickingTaskId, claim.Qty));

        return Result.Success(picked);
    }

    private static Result<Stock> Create(
        StockId id,
        Sku sku,
        LocationId locationId,
        Batch batch,
        Expiry expiry,
        Quantity qty,
        Guid sourceGrId,
        StockStatus status)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(locationId);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(expiry);
        ArgumentNullException.ThrowIfNull(qty);

        if (sourceGrId == Guid.Empty)
        {
            return Result.Invalid<Stock>(new Error("stock.source_gr_required", "SourceGrId wajib diisi."));
        }

        var stock = new Stock(id, sku, locationId, batch, expiry, qty.Value, status, sourceGrId);
        stock.Raise(new StockCreated(id, sku, qty.Value, status, sourceGrId));

        return Result.Success(stock);
    }
}
