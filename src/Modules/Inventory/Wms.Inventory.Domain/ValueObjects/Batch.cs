using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain.ValueObjects;

public sealed record Batch
{
    private Batch(string value) => Value = value;

    public string Value { get; }

    public static Result<Batch> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<Batch>(new Error("batch.value_required", "Batch wajib diisi."));
        }

        return Result.Success(new Batch(value.Trim()));
    }
}
