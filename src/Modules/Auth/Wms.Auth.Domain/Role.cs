using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

// Kumpulan permission yang dimiliki oleh role.
public sealed class Role : AggregateRoot<RoleId>, IAuditable
{
    // ID permission yang dimiliki oleh role.
    private readonly List<Guid> _permissionIds;

    private Role(RoleId id, string code, string name, List<Guid> permissionIds)
        : base(id)
    {
        Code = code;
        Name = name;
        _permissionIds = permissionIds;
        IsActive = true;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private Role()
        : base(default!)
    {
        Code = null!;
        Name = null!;
        _permissionIds = [];
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<Guid> PermissionIds => _permissionIds.AsReadOnly();

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<Role> Create(RoleId id, string code, string name, IEnumerable<Guid> permissionIds)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(permissionIds);

        var error = ValidateDetails(code, name);
        if (error is not null)
        {
            return Result.Invalid<Role>(error);
        }

        return Result.Success(new Role(id, code.Trim(), name.Trim(), Snapshot(permissionIds)));
    }

    public Result Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Invalid(new Error("role.name_required", "Nama role wajib diisi."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public Result SetPermissions(IEnumerable<Guid> permissionIds)
    {
        ArgumentNullException.ThrowIfNull(permissionIds);

        _permissionIds.Clear();
        _permissionIds.AddRange(Snapshot(permissionIds));
        return Result.Success();
    }

    // Soft delete, idempotent.
    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }

    private static Error? ValidateDetails(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new Error("role.code_required", "Code role wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return new Error("role.name_required", "Nama role wajib diisi.");
        }

        return null;
    }

    private static List<Guid> Snapshot(IEnumerable<Guid> permissionIds) =>
        permissionIds.Where(id => id != Guid.Empty).Distinct().ToList();
}
