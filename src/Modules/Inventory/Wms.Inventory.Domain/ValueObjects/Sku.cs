using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain.ValueObjects;

// Business key produk
public sealed record Sku
{
    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Result<Sku> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<Sku>(new Error("sku.value_required", "SKU wajib diisi."));
        }

        return Result.Success(new Sku(value.Trim()));
    }
}
