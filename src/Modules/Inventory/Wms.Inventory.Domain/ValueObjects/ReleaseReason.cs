using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain.ValueObjects;

// Alasan reservasi dilepas (wave cancel / manual release)
public sealed record ReleaseReason
{
    private ReleaseReason(string value) => Value = value;

    public string Value { get; }

    public static Result<ReleaseReason> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<ReleaseReason>(new Error("release_reason.value_required", "ReleaseReason wajib diisi."));
        }

        return Result.Success(new ReleaseReason(value.Trim()));
    }
}
