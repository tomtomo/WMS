using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Product.UpdateProduct;

[RequiresPermission(MasterDataPermissions.ManageProduct)]
public sealed record UpdateProductCommand(
    string Sku,
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays) : ICommand;
