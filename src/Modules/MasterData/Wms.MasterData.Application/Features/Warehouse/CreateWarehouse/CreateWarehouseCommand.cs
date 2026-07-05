using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Warehouse.CreateWarehouse;

// Admin master data membuat Warehouse.
[RequiresPermission(MasterDataPermissions.ManageWarehouse)]
public sealed record CreateWarehouseCommand(string Name, string Address) : ICommand<Guid>;
