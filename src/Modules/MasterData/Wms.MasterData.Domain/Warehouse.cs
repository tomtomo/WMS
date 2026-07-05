using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.MasterData.Domain;

// Gudang fisik tempat operasional WMS
public sealed class Warehouse : AggregateRoot<WarehouseId>, IAuditable
{
    private Warehouse(WarehouseId id, string name, string address)
        : base(id)
    {
        Name = name;
        Address = address;
        IsActive = true;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private Warehouse()
        : base(default!)
    {
        Name = null!;
        Address = null!;
    }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public bool IsActive { get; private set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<Warehouse> Create(WarehouseId id, string name, string address)
    {
        ArgumentNullException.ThrowIfNull(id);

        var error = ValidateDetails(name, address);
        if (error is not null)
        {
            return Result.Invalid<Warehouse>(error);
        }

        return Result.Success(new Warehouse(id, name.Trim(), address.Trim()));
    }

    public Result Update(string name, string address)
    {
        var error = ValidateDetails(name, address);
        if (error is not null)
        {
            return Result.Invalid(error);
        }

        Name = name.Trim();
        Address = address.Trim();
        return Result.Success();
    }

    // Softdelete, idempotent
    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }

    private static Error? ValidateDetails(string name, string address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new Error("warehouse.name_required", "Nama warehouse wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            return new Error("warehouse.address_required", "Alamat warehouse wajib diisi.");
        }

        return null;
    }
}
