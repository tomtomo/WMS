namespace Wms.Auth.Application.ReadModels;

// Read DTO User
public sealed record UserDto(
    Guid UserId,
    string Username,
    string Email,
    bool IsActive,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<Guid> AssignedWarehouseIds,
    IReadOnlyList<string> PermissionCodes);
