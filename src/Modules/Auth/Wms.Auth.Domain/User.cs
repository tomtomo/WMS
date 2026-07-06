using System.Diagnostics.CodeAnalysis;
using Wms.Auth.Domain.ValueObjects;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Primitives;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Domain;

// User yang dapat mengakses sistem.
public sealed class User : AggregateRoot<UserId>, IAuditable
{
    // ID role yang dimiliki oleh User.
    private readonly List<Guid> _roleIds;

    private readonly List<Guid> _assignedWarehouseIds;

    private User(
        UserId id,
        string username,
        string email,
        string passwordHash,
        List<Guid> roleIds,
        List<Guid> assignedWarehouseIds)
        : base(id)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        _roleIds = roleIds;
        _assignedWarehouseIds = assignedWarehouseIds;
        Status = UserStatus.Active;
        FailedLoginCount = 0;
        IsActive = true;
    }

    [SuppressMessage(
        "Major Code Smell",
        "S1144:Unused private types or members should be removed",
        Justification = "Dipanggil EF Core lewat reflection saat materialization — pola DDD dan EF standar.")]
    private User()
        : base(default!)
    {
        Username = null!;
        Email = null!;
        PasswordHash = null!;
        _roleIds = [];
        _assignedWarehouseIds = [];
    }

    public string Username { get; private set; }

    public string Email { get; private set; }

    // Password disimpan dalam bentuk hash.
    public string PasswordHash { get; private set; }

    public UserStatus Status { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockedAt { get; private set; }

    // Menandakan apakah user masih aktif.
    public bool IsActive { get; private set; }

    public IReadOnlyList<Guid> RoleIds => _roleIds.AsReadOnly();

    public IReadOnlyList<Guid> AssignedWarehouseIds => _assignedWarehouseIds.AsReadOnly();

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public static Result<User> Create(
        UserId id,
        string username,
        string email,
        string passwordHash,
        IEnumerable<Guid> roleIds,
        IEnumerable<Guid> assignedWarehouseIds)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(roleIds);
        ArgumentNullException.ThrowIfNull(assignedWarehouseIds);

        var error = ValidateDetails(username, email, passwordHash);
        if (error is not null)
        {
            return Result.Invalid<User>(error);
        }

        var roleSnapshot = roleIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var warehouseSnapshot = assignedWarehouseIds.Where(id => id != Guid.Empty).Distinct().ToList();

        return Result.Success(new User(id, username.Trim(), email.Trim(), passwordHash, roleSnapshot, warehouseSnapshot));
    }

    // percobaan login yang gagal dan terapkan lockout jika diperlukan.
    public Result RecordFailedLogin(DateTimeOffset occurredAt)
    {
        if (Status == UserStatus.Disabled)
        {
            return Result.Invalid(new Error("user.disabled", "User nonaktif tidak dapat mencatat percobaan login."));
        }

        FailedLoginCount++;

        if (Status == UserStatus.Active && FailedLoginCount >= LockoutPolicy.Default.MaxFailedAttempts)
        {
            Status = UserStatus.Locked;
            LockedAt = occurredAt;
        }

        return Result.Success();
    }

    // Reset status percobaan login setelah berhasil login.
    public Result RecordSuccessfulLogin()
    {
        if (Status == UserStatus.Disabled)
        {
            return Result.Invalid(new Error("user.disabled", "User nonaktif tidak dapat login."));
        }

        if (Status == UserStatus.Locked)
        {
            return Result.Conflict(new Error("user.locked", "User terkunci; buka kunci sebelum login."));
        }

        FailedLoginCount = 0;
        LockedAt = null;
        return Result.Success();
    }

    // Cek apakah lockout sudah berakhir.
    public bool IsLockoutExpired(DateTimeOffset now) =>
        Status == UserStatus.Locked
        && LockedAt is not null
        && LockedAt.Value + LockoutPolicy.Default.LockoutDuration <= now;

    // Kembalikan user ke status aktif.
    public Result Unlock()
    {
        if (Status == UserStatus.Disabled)
        {
            return Result.Invalid(new Error("user.disabled", "User nonaktif tidak dapat dibuka kuncinya."));
        }

        Status = UserStatus.Active;
        FailedLoginCount = 0;
        LockedAt = null;
        return Result.Success();
    }

    public Result Disable()
    {
        Status = UserStatus.Disabled;
        IsActive = false;
        return Result.Success();
    }

    public Result Enable()
    {
        Status = UserStatus.Active;
        IsActive = true;
        FailedLoginCount = 0;
        LockedAt = null;
        return Result.Success();
    }

    public Result AssignRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            return Result.Invalid(new Error("user.role_invalid", "RoleId tidak boleh kosong."));
        }

        if (!_roleIds.Contains(roleId))
        {
            _roleIds.Add(roleId);
        }

        return Result.Success();
    }

    public Result AssignWarehouse(Guid warehouseId)
    {
        if (warehouseId == Guid.Empty)
        {
            return Result.Invalid(new Error("user.warehouse_invalid", "WarehouseId tidak boleh kosong."));
        }

        if (!_assignedWarehouseIds.Contains(warehouseId))
        {
            _assignedWarehouseIds.Add(warehouseId);
        }

        return Result.Success();
    }

    private static Error? ValidateDetails(string username, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return new Error("user.username_required", "Username wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return new Error("user.email_required", "Email wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return new Error("user.password_hash_required", "PasswordHash wajib diisi.");
        }

        return null;
    }
}
