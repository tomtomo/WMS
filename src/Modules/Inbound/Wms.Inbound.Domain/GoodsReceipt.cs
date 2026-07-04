using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.Events;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Domain;

// Penerimaan satu pengiriman supplier.
public sealed class GoodsReceipt : AggregateRoot<GoodsReceiptId>, IAuditable
{
    private readonly List<ExpectedLine> _expectedLines;

    private readonly List<ScannedLine> _scannedLines = [];

    private readonly List<QuantityCheck> _quantityChecks = [];

    private readonly List<Discrepancy> _discrepancies = [];

    private readonly List<Resolution> _resolutions = [];

    private readonly List<ReceivedLine> _receivedLines = [];

    private readonly List<RejectedLine> _rejectedLines = [];

    private GoodsReceipt(
        GoodsReceiptId id,
        string poRef,
        Guid supplierId,
        Guid warehouseId,
        DockDoor dockDoor,
        List<ExpectedLine> expectedLines)
        : base(id)
    {
        PoRef = poRef;
        SupplierId = supplierId;
        WarehouseId = warehouseId;
        DockDoor = dockDoor;
        _expectedLines = expectedLines;
        Status = GoodsReceiptStatus.InProgress;
    }

    // Ctor materialization EF (bukan jalur bisnis) — kolom & koleksi diisi via backing field.
    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private GoodsReceipt()
        : base(default!)
    {
        PoRef = string.Empty;
        DockDoor = null!;
        _expectedLines = [];
    }

    public string PoRef { get; }

    public Guid SupplierId { get; }

    public Guid WarehouseId { get; }

    public DockDoor DockDoor { get; }

    public GoodsReceiptStatus Status { get; private set; }

    public HoldReason? HoldReason { get; private set; }

    public IReadOnlyList<ExpectedLine> ExpectedLines => _expectedLines.AsReadOnly();

    public IReadOnlyList<ScannedLine> ScannedLines => _scannedLines.AsReadOnly();

    public IReadOnlyList<QuantityCheck> QuantityChecks => _quantityChecks.AsReadOnly();

    public IReadOnlyList<Discrepancy> Discrepancies => _discrepancies.AsReadOnly();

    public IReadOnlyList<Resolution> Resolutions => _resolutions.AsReadOnly();

    public IReadOnlyList<ReceivedLine> ReceivedLines => _receivedLines.AsReadOnly();

    public IReadOnlyList<RejectedLine> RejectedLines => _rejectedLines.AsReadOnly();

    // IAuditable — diisi EF SaveChanges interceptor dari ICurrentUser, bukan oleh domain.
    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    // SupplierId mandatory
    public static Result<GoodsReceipt> Create(
        GoodsReceiptId id,
        string poRef,
        Guid supplierId,
        Guid warehouseId,
        DockDoor dockDoor,
        IEnumerable<ExpectedLine> expectedLines)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(dockDoor);
        ArgumentNullException.ThrowIfNull(expectedLines);

        if (string.IsNullOrWhiteSpace(poRef))
        {
            return Result.Invalid<GoodsReceipt>(new Error("goods_receipt.po_ref_required", "Referensi PO wajib diisi."));
        }

        if (supplierId == Guid.Empty)
        {
            return Result.Invalid<GoodsReceipt>(new Error("goods_receipt.supplier_required", "SupplierId wajib diisi."));
        }

        if (warehouseId == Guid.Empty)
        {
            return Result.Invalid<GoodsReceipt>(new Error("goods_receipt.warehouse_required", "WarehouseId wajib diisi."));
        }

        var snapshot = expectedLines.ToList();
        if (snapshot.Count == 0)
        {
            return Result.Invalid<GoodsReceipt>(new Error("goods_receipt.expected_lines_required", "GR harus punya minimal satu expected line."));
        }

        // SKU expected wajib unik
        if (snapshot.GroupBy(line => line.Sku, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            return Result.Invalid<GoodsReceipt>(new Error("goods_receipt.expected_sku_duplicated", "SKU expected tidak boleh duplikat."));
        }

        return Result.Success(new GoodsReceipt(id, poRef.Trim(), supplierId, warehouseId, dockDoor, snapshot));
    }

    // SKU di luar expected wajib ter tag WrongItem.
    public Result Scan(ScannedLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (Status != GoodsReceiptStatus.InProgress)
        {
            return Result.Conflict(new Error("goods_receipt.not_in_progress", "Scan hanya bisa saat GR InProgress."));
        }

        if (line.LineStatus != LineStatus.WrongItem && !IsExpectedSku(line.Sku))
        {
            return Result.Invalid(new Error("goods_receipt.unexpected_sku", "SKU tidak ada di expected lines; tag sebagai WrongItem."));
        }

        // Urutan penerimaan, karena tidak boleh bergantung urutan load DB.
        _scannedLines.Add(line with { ScanSequence = _scannedLines.Count });
        return Result.Success();
    }

    // Dua sumbu independen di compile jadi discrepancies
    public Result CompleteScan()
    {
        if (Status != GoodsReceiptStatus.InProgress)
        {
            return Result.Conflict(new Error("goods_receipt.not_in_progress", "Scan hanya bisa diselesaikan saat GR InProgress."));
        }

        foreach (var expected in _expectedLines)
        {
            // QcHold ikut dihitung ke qty, WrongItem dikecualikan.
            var actualQty = AcceptableLinesOf(expected.Sku).Sum(line => line.ActualQty);

            _quantityChecks.Add(QuantityCheck.Compute(expected.Sku, expected.ExpectedQty, actualQty));
        }

        foreach (var check in _quantityChecks.Where(check => check.Variance != QuantityVariance.Normal))
        {
            var type = check.Variance == QuantityVariance.ShortDelivery
                ? DiscrepancyType.ShortDelivery
                : DiscrepancyType.OverDelivery;

            _discrepancies.Add(new Discrepancy(Guid.NewGuid(), check.Sku, type, Math.Abs(check.ActualQty - check.ExpectedQty)));
        }

        foreach (var group in _scannedLines
            .Where(line => line.LineStatus != LineStatus.Good)
            .GroupBy(line => (line.Sku, line.LineStatus)))
        {
            var type = group.Key.LineStatus == LineStatus.QcHold ? DiscrepancyType.QcHold : DiscrepancyType.WrongItem;
            _discrepancies.Add(new Discrepancy(Guid.NewGuid(), group.Key.Sku, type, group.Sum(line => line.ActualQty)));
        }

        Status = GoodsReceiptStatus.Pending;

        var hasOverDelivery = _quantityChecks.Exists(check => check.Variance == QuantityVariance.OverDelivery);
        Raise(new GoodsReceiptPendingReviewRaised(Id, WarehouseId, hasOverDelivery, _discrepancies.Count));

        return Result.Success();
    }

    // Action wajib sesuai type discrepancy, resolve sebelum Confirm
    public Result Resolve(Guid discrepancyId, ResolutionAction action, string? note = null)
    {
        if (Status != GoodsReceiptStatus.Pending)
        {
            return Result.Conflict(new Error("goods_receipt.not_pending", "Resolve hanya bisa saat GR Pending."));
        }

        var discrepancy = _discrepancies.Find(candidate => candidate.Id == discrepancyId);
        if (discrepancy is null)
        {
            return Result.NotFound(new Error("goods_receipt.discrepancy_not_found", "Discrepancy tidak ditemukan di GR ini."));
        }

        if (RequiredActionFor(discrepancy.Type) != action)
        {
            return Result.Invalid(new Error("goods_receipt.resolution_action_mismatch", "Action tidak sesuai type discrepancy."));
        }

        _resolutions.RemoveAll(existing => existing.DiscrepancyId == discrepancyId);
        _resolutions.Add(new Resolution(discrepancyId, action, note));

        return Result.Success();
    }

    // Hanya bisa diposting jka tiap discrepancy sudah punya resolution. Hasil = receivedLines/rejectedLines.
    public Result Confirm()
    {
        if (Status != GoodsReceiptStatus.Pending)
        {
            return Result.Conflict(new Error("goods_receipt.not_pending", "Confirm hanya bisa saat GR Pending."));
        }

        if (_discrepancies.Exists(discrepancy => !_resolutions.Exists(resolution => resolution.DiscrepancyId == discrepancy.Id)))
        {
            return Result.Invalid(new Error("goods_receipt.discrepancy_unresolved", "Semua discrepancy harus punya resolution sebelum Confirm."));
        }

        BuildOutcomeLines();
        Status = GoodsReceiptStatus.Confirmed;
        Raise(new GoodsReceiptConfirmed(Id, WarehouseId, SupplierId, [.. _receivedLines], [.. _rejectedLines]));

        return Result.Success();
    }

    // Terminal
    public Result Hold(HoldReason reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (Status != GoodsReceiptStatus.Pending)
        {
            return Result.Conflict(new Error("goods_receipt.not_pending", "Hold hanya bisa saat GR Pending."));
        }

        HoldReason = reason;
        Status = GoodsReceiptStatus.Hold;
        Raise(new GoodsReceiptHeld(Id, WarehouseId, reason.Value));

        return Result.Success();
    }

    // Satu action valid per type
    private static ResolutionAction RequiredActionFor(DiscrepancyType type) => type switch
    {
        DiscrepancyType.ShortDelivery => ResolutionAction.AcceptPartial,
        DiscrepancyType.OverDelivery => ResolutionAction.RejectExcess,
        DiscrepancyType.WrongItem => ResolutionAction.ReturnToSupplier,
        DiscrepancyType.QcHold => ResolutionAction.SendToQC,
        _ => throw new UnreachableException($"DiscrepancyType tak dikenal: {type}"),
    };

    private bool IsExpectedSku(string sku)
        => _expectedLines.Exists(expected => string.Equals(expected.Sku, sku, StringComparison.Ordinal));

    private List<ScannedLine> AcceptableLinesOf(string sku)
        => [.. _scannedLines
            .Where(line => string.Equals(line.Sku, sku, StringComparison.Ordinal) && line.LineStatus != LineStatus.WrongItem)
            .OrderBy(line => line.ScanSequence)];

    // AcceptPartial: terima apa adanya, RejectExcess: cap di expectedQty
    // SendToQC: terima berstatus QcHold, ReturnToSupplier: seluruh line WrongItem ke rejected.
    private void BuildOutcomeLines()
    {
        foreach (var expected in _expectedLines)
        {
            var acceptable = AcceptableLinesOf(expected.Sku);
            var scannedTotal = acceptable.Sum(line => line.ActualQty);
            var cap = Math.Min(scannedTotal, expected.ExpectedQty);

            // QC diterima lebih dulu
            var budget = AcceptInScanOrder(acceptable.FindAll(line => line.LineStatus == LineStatus.QcHold), ReceivedLineStatus.QcHold, cap);
            AcceptInScanOrder(acceptable.FindAll(line => line.LineStatus == LineStatus.Good), ReceivedLineStatus.Good, budget);

            var excess = scannedTotal - cap;
            if (excess > 0)
            {
                _rejectedLines.Add(new RejectedLine(expected.Sku, excess, RejectionReason.OverDelivery));
            }
        }

        foreach (var strayGroup in _scannedLines
            .Where(line => line.LineStatus == LineStatus.WrongItem)
            .GroupBy(line => line.Sku, StringComparer.Ordinal))
        {
            _rejectedLines.Add(new RejectedLine(strayGroup.Key, strayGroup.Sum(line => line.ActualQty), RejectionReason.WrongItem));
        }
    }

    // Kumulatif urutan scan
    private decimal AcceptInScanOrder(IReadOnlyList<ScannedLine> lines, ReceivedLineStatus status, decimal budget)
    {
        foreach (var line in lines)
        {
            if (budget <= 0)
            {
                break;
            }

            var acceptedQty = Math.Min(line.ActualQty, budget);
            budget -= acceptedQty;
            AddOrMergeReceived(new ReceivedLine(line.Sku, acceptedQty, line.Batch, line.Expiry, status));
        }

        return budget;
    }

    // Merge (sku, batch, expiry, status) agar dua scan batch sama tidak jadi dua line.
    private void AddOrMergeReceived(ReceivedLine line)
    {
        var index = _receivedLines.FindIndex(existing =>
            string.Equals(existing.Sku, line.Sku, StringComparison.Ordinal)
            && string.Equals(existing.Batch, line.Batch, StringComparison.Ordinal)
            && existing.Expiry == line.Expiry
            && existing.Status == line.Status);

        if (index >= 0)
        {
            _receivedLines[index] = _receivedLines[index] with { Qty = _receivedLines[index].Qty + line.Qty };
        }
        else
        {
            _receivedLines.Add(line);
        }
    }
}
