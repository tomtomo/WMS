using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Warehouse.DeactivateWarehouse;

// Soft delete Warehouse
[RequiresPermission(MasterDataPermissions.ManageWarehouse)]
public sealed record DeactivateWarehouseCommand(Guid WarehouseId) : ICommand;
