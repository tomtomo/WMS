using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Wms.Auth.Domain;
using Wms.Auth.Domain.ValueObjects;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.Infrastructure.Seed;

// Menyiapkan data awal untuk modul Auth.
public static class AuthSeeder
{
    public const string DefaultAdminUsername = "admin";

    // Password default untuk admin.
    [SuppressMessage(
        "Security Hotspot",
        "S2068:Hard-coded credentials are security-sensitive",
        Justification = "Default dev-seed sandbox; didokumentasikan wajib diganti saat produksi.")]
    public const string DefaultAdminPassword = "ChangeMe#2026";

    // User Viewer untuk test E2E dengan akses read only. Khusus seed development.
    public const string DefaultViewerUsername = "viewer";

    [SuppressMessage(
        "Security Hotspot",
        "S2068:Hard-coded credentials are security-sensitive",
        Justification = "Default dev-seed sandbox; didokumentasikan wajib diganti saat produksi.")]
    public const string DefaultViewerPassword = "ChangeMe#2026";

    private const string AdminRoleCode = "Admin";

    private const string ViewerRoleCode = "Viewer";

    private const string SupervisorRoleCode = "Supervisor";

    // GUID tetap untuk role Supervisor. Nilainya harus sama dengan NotificationRoles.Supervisor agar pencarian anggota role lewat gRPC tetap bekerja.
    private static readonly Guid _supervisorRoleId = Guid.Parse("a1a1a1a1-0000-0000-0000-000000000001");

    // Warehouse default untuk data lokal. Admin dimasukkan ke warehouse ini agar akses data dev tidak gagal karena belum memiliki claim multi warehouse.
    private static readonly Guid _defaultDevWarehouseId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    // Daftar permission yang dikenali aplikasi.
    private static readonly (string Code, string Description)[] _catalog =
    [
        ("Inbound.CreateGR", "Buat Goods Receipt"),
        ("Inbound.ScanGR", "Scan item saat receiving"),
        ("Inbound.CompleteScanGR", "Selesaikan scan Goods Receipt"),
        ("Inbound.ResolveGR", "Selesaikan discrepancy GR"),
        ("Inbound.PostGR", "Posting/konfirmasi Goods Receipt"),
        ("Inbound.HoldGR", "Tahan Goods Receipt"),
        ("Inbound.UploadGRAttachment", "Unggah attachment GR"),
        ("Inbound.DeleteGRAttachment", "Hapus attachment GR"),
        ("Inbound.ReadGR", "Lihat Goods Receipt"),
        ("Inventory.CompletePutaway", "Selesaikan putaway"),
        ("Inventory.ReadStock", "Lihat stok"),
        ("Inventory.AdjustStock", "Penyesuaian stok"),
        ("Outbound.CreateOrder", "Buat outbound order"),
        ("Outbound.CreateWave", "Buat wave"),
        ("Outbound.CompletePickingTask", "Selesaikan picking task"),
        ("Outbound.DispatchWave", "Dispatch wave"),
        ("Outbound.Read", "Lihat outbound"),
        ("MasterData.ManageWarehouse", "Kelola warehouse"),
        ("MasterData.ManageLocation", "Kelola location"),
        ("MasterData.ManageProduct", "Kelola product"),
        ("MasterData.Read", "Lihat master data"),
        ("Auth.ManageUser", "Kelola user"),
        ("Auth.ManageRole", "Kelola role"),
        ("Auth.AssignPermission", "Assign permission ke role"),
    ];

    // Role default
    private static readonly (string Code, string Name, string[] Permissions)[] _defaultRoles =
    [
        (AdminRoleCode, "Administrator", []),
        ("WarehouseManager", "Warehouse Manager",
        [
            "Inbound.CreateGR", "Inbound.ScanGR", "Inbound.CompleteScanGR", "Inbound.ResolveGR", "Inbound.PostGR",
            "Inbound.HoldGR", "Inbound.UploadGRAttachment", "Inbound.DeleteGRAttachment", "Inbound.ReadGR",
            "Inventory.CompletePutaway", "Inventory.AdjustStock", "Inventory.ReadStock",
            "Outbound.CreateWave", "Outbound.CompletePickingTask", "Outbound.DispatchWave", "Outbound.Read",
            "MasterData.ManageWarehouse", "MasterData.ManageLocation", "MasterData.ManageProduct", "MasterData.Read",
        ]),
        ("Supervisor", "Supervisor",
        [
            "Inbound.PostGR", "Inbound.HoldGR", "Inbound.ResolveGR", "Inbound.ReadGR",
            "Inventory.ReadStock",
            "Outbound.CreateWave", "Outbound.DispatchWave", "Outbound.Read",
            "MasterData.Read",
        ]),
        ("Operator", "Operator",
        [
            "Inbound.CreateGR", "Inbound.ScanGR", "Inbound.CompleteScanGR", "Inbound.ReadGR",
            "Inventory.CompletePutaway", "Inventory.ReadStock",
            "Outbound.CompletePickingTask", "Outbound.Read",
        ]),
        ("Viewer", "Viewer",
        [
            "Inbound.ReadGR", "Inventory.ReadStock", "Outbound.Read", "MasterData.Read",
        ]),
    ];

    public static async Task SeedAsync(
        AuthDbContext context,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        var permissionsByCode = await SeedPermissionsAsync(context, cancellationToken);
        await SeedRolesAsync(context, permissionsByCode, cancellationToken);
        await SeedAdminAsync(context, passwordHasher, cancellationToken);
        await SeedLowPermUserAsync(context, passwordHasher, cancellationToken);
    }

    // Kode katalog ter seed
    public static IReadOnlyList<string> CatalogCodes() => Array.ConvertAll(_catalog, entry => entry.Code);

    private static async Task<Dictionary<string, Guid>> SeedPermissionsAsync(AuthDbContext context, CancellationToken cancellationToken)
    {
        var existingCodes = (await context.Set<Permission>().ToListAsync(cancellationToken))
            .Select(permission => permission.Code.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (code, description) in _catalog)
        {
            if (existingCodes.Contains(code))
            {
                continue;
            }

            var permission = Permission.Create(
                PermissionId.Create(Guid.NewGuid()).Value, PermissionCode.Create(code).Value, description);
            context.Add(permission.Value);
        }

        await context.SaveChangesAsync(cancellationToken);

        return (await context.Set<Permission>().ToListAsync(cancellationToken))
            .ToDictionary(permission => permission.Code.Value, permission => permission.Id.Value, StringComparer.Ordinal);
    }

    private static async Task SeedRolesAsync(
        AuthDbContext context,
        IReadOnlyDictionary<string, Guid> permissionsByCode,
        CancellationToken cancellationToken)
    {
        var allPermissionIds = permissionsByCode.Values.ToList();

        foreach (var (code, name, permissionCodes) in _defaultRoles)
        {
            var existing = await context.Set<Role>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(role => role.Code == code, cancellationToken);
            if (existing is not null)
            {
                // Pastikan role Admin selalu memiliki semua permission, termasuk permission baru saat memakai database lama.
                if (string.Equals(code, AdminRoleCode, StringComparison.Ordinal)
                    && allPermissionIds.Except(existing.PermissionIds).Any())
                {
                    existing.SetPermissions(allPermissionIds);
                }

                continue;
            }

            var permissionIds = string.Equals(code, AdminRoleCode, StringComparison.Ordinal)
                ? allPermissionIds
                : permissionCodes.Select(permissionCode => permissionsByCode[permissionCode]).ToList();

            // Role Supervisor memakai GUID tetap agar notifikasi berdasarkan role bisa menemukan anggotanya, role lain tetap memakai GUID acak.
            var roleId = string.Equals(code, SupervisorRoleCode, StringComparison.Ordinal) ? _supervisorRoleId : Guid.NewGuid();
            context.Add(Role.Create(RoleId.Create(roleId).Value, code, name, permissionIds).Value);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminAsync(AuthDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        if (await context.Set<User>().IgnoreQueryFilters().AnyAsync(user => user.Username == DefaultAdminUsername, cancellationToken))
        {
            return;
        }

        var adminRole = await context.Set<Role>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(role => role.Code == AdminRoleCode, cancellationToken);
        if (adminRole is null)
        {
            return;
        }

        var passwordHash = passwordHasher.Hash(DefaultAdminPassword);

        // Admin juga diberi role Supervisor agar menerima notifikasi SPV. Di sandbox ini admin merangkap SPV, tetapi tetap memiliki seluruh permission Admin.
        var admin = User.Create(
            UserId.Create(Guid.NewGuid()).Value,
            DefaultAdminUsername,
            "admin@wms.local",
            passwordHash,
            [adminRole.Id.Value, _supervisorRoleId],
            [_defaultDevWarehouseId]);
        context.Add(admin.Value);
        await context.SaveChangesAsync(cancellationToken);
    }

    // Seed user Viewer dengan akses read only
    private static async Task SeedLowPermUserAsync(AuthDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken)
    {
        if (await context.Set<User>().IgnoreQueryFilters().AnyAsync(user => user.Username == DefaultViewerUsername, cancellationToken))
        {
            return;
        }

        var viewerRole = await context.Set<Role>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(role => role.Code == ViewerRoleCode, cancellationToken);
        if (viewerRole is null)
        {
            return;
        }

        var passwordHash = passwordHasher.Hash(DefaultViewerPassword);
        var viewer = User.Create(
            UserId.Create(Guid.NewGuid()).Value,
            DefaultViewerUsername,
            "viewer@wms.local",
            passwordHash,
            [viewerRole.Id.Value],
            [_defaultDevWarehouseId]);
        context.Add(viewer.Value);
        await context.SaveChangesAsync(cancellationToken);
    }
}
