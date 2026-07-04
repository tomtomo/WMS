using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain;

// Klaim qty terhadap satu Stock Available untuk satu wave.
public sealed class StockReservation : AggregateRoot<StockReservationId>, IAuditable
{
    private StockReservation(
        StockReservationId id,
        StockId stockId,
        Guid waveId,
        Guid orderId,
        Sku sku,
        Batch batch,
        decimal qty)
        : base(id)
    {
        StockId = stockId;
        WaveId = waveId;
        OrderId = orderId;
        Sku = sku;
        Batch = batch;
        Qty = qty;
        Status = ReservationStatus.Active;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private StockReservation()
        : base(default!)
    {
        StockId = null!;
        Sku = null!;
        Batch = null!;
    }

    public StockId StockId { get; }

    public Guid WaveId { get; }

    public Guid OrderId { get; }

    public Sku Sku { get; }

    public Batch Batch { get; }

    public decimal Qty { get; }

    public ReservationStatus Status { get; private set; }

    public Guid? PickingTaskId { get; private set; }

    public ReleaseReason? ReleaseReason { get; private set; }

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<StockReservation> Create(
        StockReservationId id,
        StockId stockId,
        Guid waveId,
        Guid orderId,
        Sku sku,
        Batch batch,
        Quantity qty)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(stockId);
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(qty);

        if (waveId == Guid.Empty)
        {
            return Result.Invalid<StockReservation>(new Error("stock_reservation.wave_required", "WaveId wajib diisi."));
        }

        if (orderId == Guid.Empty)
        {
            return Result.Invalid<StockReservation>(new Error("stock_reservation.order_required", "OrderId wajib diisi."));
        }

        return Result.Success(new StockReservation(id, stockId, waveId, orderId, sku, batch, qty.Value));
    }

    // Picking selesai: reservasi ditutup, qty yang terreservasi telah diambil fisik ke staging.
    public Result Fulfill(Guid pickingTaskId)
    {
        if (Status != ReservationStatus.Active)
        {
            return Result.Conflict(new Error("stock_reservation.not_active", "Fulfill hanya bisa saat reservasi Active."));
        }

        PickingTaskId = pickingTaskId;
        Status = ReservationStatus.Fulfilled;

        return Result.Success();
    }

    // Reservasi dilepas (wave cancel / manual release), availableQty balik di sisi Stock.
    public Result Release(ReleaseReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (Status != ReservationStatus.Active)
        {
            return Result.Conflict(new Error("stock_reservation.not_active", "Release hanya bisa saat reservasi Active."));
        }

        ReleaseReason = reason;
        Status = ReservationStatus.Released;

        return Result.Success();
    }
}
