namespace Wms.BuildingBlocks.Domain.Results;

// Bisa map ke status berbeda (400/403/404/409).
public enum ResultErrorType
{
    None,
    Failure,
    Validation,
    Conflict,
    NotFound,

    // AuthZ gagal: permission absen → REST 403 / gRPC PermissionDenied.
    Forbidden,
}
