using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Warehouse.UpdateWarehouse;

[RequiresPermission(MasterDataPermissions.ManageWarehouse)]
public sealed record UpdateWarehouseCommand(Guid WarehouseId, string Name, string Address) : ICommand;
