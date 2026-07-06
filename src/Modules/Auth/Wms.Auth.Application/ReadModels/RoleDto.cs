namespace Wms.Auth.Application.ReadModels;

// Read DTO Role
public sealed record RoleDto(
    Guid RoleId,
    string Code,
    string Name,
    bool IsActive,
    IReadOnlyList<string> PermissionCodes);
