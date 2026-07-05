namespace Wms.MasterData.Application.ReadModels;

// Read DTO Location
public sealed record LocationDto(Guid LocationId, Guid WarehouseId, string Type, string Code, bool IsActive);
