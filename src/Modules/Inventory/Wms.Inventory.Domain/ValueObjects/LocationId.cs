using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inventory.Domain.ValueObjects;

// Referensi lokasi fisik (receiving/rack/staging/quarantine) — dimiliki master Warehouse. VO referensi.
public sealed record LocationId
{
    private LocationId(Guid value) => Value = value;

    public Guid Value { get; }

    public static Result<LocationId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Result.Invalid<LocationId>(new Error("location_id.value_required", "LocationId wajib diisi."));
        }

        return Result.Success(new LocationId(value));
    }
}
