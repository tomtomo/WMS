namespace Wms.BuildingBlocks.Domain.Results;

// Bisa map ke status berbeda (400/409/404).
public enum ResultErrorType
{
    None,
    Failure,
    Validation,
    Conflict,
    NotFound,
}
