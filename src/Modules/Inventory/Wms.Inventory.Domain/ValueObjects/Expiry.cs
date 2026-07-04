using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain.ValueObjects;

// Tanggal kedaluwarsa batch — dasar urutan FEFO
public sealed record Expiry
{
    private Expiry(DateOnly value) => Value = value;

    public DateOnly Value { get; }

    public static Result<Expiry> Create(DateOnly value)
    {
        if (value == default)
        {
            return Result.Invalid<Expiry>(new Error("expiry.value_required", "Expiry wajib diisi."));
        }

        return Result.Success(new Expiry(value));
    }
}
