using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Application.Features.Product.UpdateProduct;

internal sealed class UpdateProductHandler(IProductRepository repository)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sku = Sku.Create(command.Sku);
        if (sku.IsFailure)
        {
            return sku;
        }

        var product = await repository.GetAsync(sku.Value, cancellationToken);
        if (product is null)
        {
            return Result.NotFound(new Error("product.not_found", "Product tidak ditemukan."));
        }

        return product.Update(
            command.Name,
            command.Uom,
            command.BatchTrackingRequired,
            command.ExpiryTrackingRequired,
            command.QcRequiredOnReceipt,
            command.ShelfLifeDays);
    }
}
