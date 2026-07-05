using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;

namespace Wms.Outbound.Domain.ValueObjects;

// Demand per SKU.
public sealed record OrderLine
{
    private OrderLine(string sku, decimal qty, Uom uom, decimal allocatedQty, AllocationStatus allocationStatus)
    {
        Sku = sku;
        Qty = qty;
        Uom = uom;
        AllocatedQty = allocatedQty;
        AllocationStatus = allocationStatus;
    }

    public string Sku { get; }

    public decimal Qty { get; }

    public Uom Uom { get; }

    public decimal AllocatedQty { get; internal init; }

    public AllocationStatus AllocationStatus { get; internal init; }

    public static Result<OrderLine> Create(string sku, decimal qty, Uom uom)
    {
        ArgumentNullException.ThrowIfNull(uom);

        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Invalid<OrderLine>(new Error("order_line.sku_required", "SKU wajib diisi."));
        }

        if (qty <= 0)
        {
            return Result.Invalid<OrderLine>(new Error("order_line.qty_invalid", "Qty harus lebih dari nol."));
        }

        return Result.Success(new OrderLine(sku.Trim(), qty, uom, 0m, AllocationStatus.Pending));
    }
}
