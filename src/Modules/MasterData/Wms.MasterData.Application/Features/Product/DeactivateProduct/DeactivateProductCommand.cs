using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Product.DeactivateProduct;

// Soft delete Product
[RequiresPermission(MasterDataPermissions.ManageProduct)]
public sealed record DeactivateProductCommand(string Sku) : ICommand;
