using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain.ValueObjects;

// Qty input — decimal (UOM termasuk kg), harus positif.
public sealed record Quantity
{
    private Quantity(decimal value) => Value = value;

    public decimal Value { get; }

    public static Result<Quantity> Create(decimal value)
    {
        if (value <= 0)
        {
            return Result.Invalid<Quantity>(new Error("quantity.must_be_positive", "Quantity harus lebih dari nol."));
        }

        return Result.Success(new Quantity(value));
    }
}
