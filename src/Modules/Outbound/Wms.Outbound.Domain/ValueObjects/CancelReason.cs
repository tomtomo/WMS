using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Outbound.Domain.ValueObjects;

// Alasan wave auto cancel (Unfulfilled).
public sealed record CancelReason
{
    private CancelReason(string value) => Value = value;

    public string Value { get; }

    public static Result<CancelReason> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<CancelReason>(new Error("cancel_reason.value_required", "CancelReason wajib diisi."));
        }

        return Result.Success(new CancelReason(value.Trim()));
    }
}
