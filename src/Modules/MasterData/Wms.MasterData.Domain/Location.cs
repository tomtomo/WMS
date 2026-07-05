using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Domain.Enums;

namespace Wms.MasterData.Domain;

// Lokasi spesifik di dalam Warehouse tempat Stock berada fisik
public sealed class Location : AggregateRoot<LocationId>, IAuditable
{
    private Location(LocationId id, WarehouseId warehouseId, LocationType type, string code)
        : base(id)
    {
        WarehouseId = warehouseId;
        Type = type;
        Code = code;
        IsActive = true;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private Location()
        : base(default!)
    {
        WarehouseId = null!;
        Code = null!;
    }

    public WarehouseId WarehouseId { get; }

    public LocationType Type { get; private set; }

    public string Code { get; private set; }

    public bool IsActive { get; private set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<Location> Create(LocationId id, WarehouseId warehouseId, LocationType type, string code)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(warehouseId);

        var error = ValidateDetails(type, code);
        if (error is not null)
        {
            return Result.Invalid<Location>(error);
        }

        return Result.Success(new Location(id, warehouseId, type, code.Trim()));
    }

    public Result Update(LocationType type, string code)
    {
        var error = ValidateDetails(type, code);
        if (error is not null)
        {
            return Result.Invalid(error);
        }

        Type = type;
        Code = code.Trim();
        return Result.Success();
    }

    // Soft delete, idempotent.
    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }

    private static Error? ValidateDetails(LocationType type, string code)
    {
        if (!Enum.IsDefined(type))
        {
            return new Error("location.type_invalid", "LocationType tidak valid.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return new Error("location.code_required", "Code location wajib diisi.");
        }

        return null;
    }
}
