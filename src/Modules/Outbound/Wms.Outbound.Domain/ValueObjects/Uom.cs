using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Outbound.Domain.ValueObjects;

// Unit of measure
public sealed record Uom
{
    private Uom(string value) => Value = value;

    public string Value { get; }

    public static Result<Uom> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<Uom>(new Error("uom.value_required", "UOM wajib diisi."));
        }

        return Result.Success(new Uom(value.Trim()));
    }
}
