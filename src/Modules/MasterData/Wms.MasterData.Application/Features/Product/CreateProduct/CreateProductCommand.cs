using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Product.CreateProduct;

[RequiresPermission(MasterDataPermissions.ManageProduct)]
public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays) : ICommand<string>;
