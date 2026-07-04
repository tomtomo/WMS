using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inbound.Domain.ValueObjects;

// Pintu dock tempat GR diterima
public sealed record DockDoor
{
    private DockDoor(string value) => Value = value;

    public string Value { get; }

    public static Result<DockDoor> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Invalid<DockDoor>(new Error("dock_door.value_required", "DockDoor wajib diisi."));
        }

        return Result.Success(new DockDoor(value.Trim()));
    }
}
