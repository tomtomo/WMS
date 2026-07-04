using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inbound.Domain.ValueObjects;

// Alasan SPV menolak seluruh GR
public sealed record HoldReason
{
    private HoldReason(string value) => Value = value;

    public string Value { get; }

    public static Result<HoldReason> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<HoldReason>(new Error("hold_reason.value_required", "HoldReason wajib diisi."));
        }

        return Result.Success(new HoldReason(value.Trim()));
    }
}
