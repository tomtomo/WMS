using System.Diagnostics.CodeAnalysis;
using Wms.Auth.Domain.ValueObjects;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

// Permission dengan kode Module.Action yang digunakan untuk otorisasi
public sealed class Permission : Entity<PermissionId>
{
    private Permission(PermissionId id, PermissionCode code, string description)
        : base(id)
    {
        Code = code;
        Description = description;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private Permission()
        : base(default!)
    {
        Code = null!;
        Description = null!;
    }

    public PermissionCode Code { get; private set; }

    public string Description { get; private set; }

    public static Result<Permission> Create(PermissionId id, PermissionCode code, string description)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(code);

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result.Invalid<Permission>(new Error("permission.description_required", "Deskripsi permission wajib diisi."));
        }

        return Result.Success(new Permission(id, code, description.Trim()));
    }
}
