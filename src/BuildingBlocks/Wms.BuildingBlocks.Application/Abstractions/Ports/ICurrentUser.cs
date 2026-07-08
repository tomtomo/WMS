namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

public interface ICurrentUser
{
    // Aktor yang dipakai saat tidak ada user terautentikasi.
    const string SystemActor = "SYSTEM";

    string UserId { get; }

    bool IsAuthenticated { get; }

    // Permission yang dimiliki user.
    IReadOnlyCollection<string> Permissions => [];

    // Warehouse yang boleh diakses user.
    IReadOnlyCollection<Guid> AssignedWarehouseIds => [];

    // SYSTEM atau anonymous tidak dibatasi warehouse.
    bool CanBypassWarehouseScope => !IsAuthenticated;

    // Cek apakah user memiliki permission.
    bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.Ordinal);
}
