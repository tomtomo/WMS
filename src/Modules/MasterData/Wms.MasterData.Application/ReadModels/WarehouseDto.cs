namespace Wms.MasterData.Application.ReadModels;

// Read DTO Warehouse
public sealed record WarehouseDto(Guid WarehouseId, string Name, string Address, bool IsActive);
