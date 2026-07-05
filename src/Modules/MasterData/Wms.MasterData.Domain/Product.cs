using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.MasterData.Domain;

// Master katalog barang/SKU
public sealed class Product : AggregateRoot<Sku>, IAuditable
{
    private Product(
        Sku sku,
        string name,
        string uom,
        bool batchTrackingRequired,
        bool expiryTrackingRequired,
        bool qcRequiredOnReceipt,
        int? shelfLifeDays)
        : base(sku)
    {
        Name = name;
        Uom = uom;
        BatchTrackingRequired = batchTrackingRequired;
        ExpiryTrackingRequired = expiryTrackingRequired;
        QcRequiredOnReceipt = qcRequiredOnReceipt;
        ShelfLifeDays = shelfLifeDays;
        IsActive = true;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private Product()
        : base(default!)
    {
        Name = null!;
        Uom = null!;
    }

    // Id (dari AggregateRoot) adalah Sku; alias eksplisit untuk keterbacaan reader/snapshot.
    public Sku Sku => Id;

    public string Name { get; private set; }

    public string Uom { get; private set; }

    public bool BatchTrackingRequired { get; private set; }

    public bool ExpiryTrackingRequired { get; private set; }

    // Flag disimpan; auto-tag QcHold saat receiving masih deferred (lihat master-data spec).
    public bool QcRequiredOnReceipt { get; private set; }

    public int? ShelfLifeDays { get; private set; }

    public bool IsActive { get; private set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<Product> Create(
        Sku sku,
        string name,
        string uom,
        bool batchTrackingRequired,
        bool expiryTrackingRequired,
        bool qcRequiredOnReceipt,
        int? shelfLifeDays)
    {
        ArgumentNullException.ThrowIfNull(sku);

        var error = ValidateDetails(name, uom, shelfLifeDays);
        if (error is not null)
        {
            return Result.Invalid<Product>(error);
        }

        return Result.Success(new Product(
            sku, name.Trim(), uom.Trim(), batchTrackingRequired, expiryTrackingRequired, qcRequiredOnReceipt, shelfLifeDays));
    }

    public Result Update(
        string name,
        string uom,
        bool batchTrackingRequired,
        bool expiryTrackingRequired,
        bool qcRequiredOnReceipt,
        int? shelfLifeDays)
    {
        var error = ValidateDetails(name, uom, shelfLifeDays);
        if (error is not null)
        {
            return Result.Invalid(error);
        }

        Name = name.Trim();
        Uom = uom.Trim();
        BatchTrackingRequired = batchTrackingRequired;
        ExpiryTrackingRequired = expiryTrackingRequired;
        QcRequiredOnReceipt = qcRequiredOnReceipt;
        ShelfLifeDays = shelfLifeDays;
        return Result.Success();
    }

    // Soft-delete (isActive=false), idempotent.
    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }

    private static Error? ValidateDetails(string name, string uom, int? shelfLifeDays)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new Error("product.name_required", "Nama product wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(uom))
        {
            return new Error("product.uom_required", "Uom product wajib diisi.");
        }

        if (shelfLifeDays is < 0)
        {
            return new Error("product.shelf_life_invalid", "ShelfLifeDays tidak boleh negatif.");
        }

        return null;
    }
}
