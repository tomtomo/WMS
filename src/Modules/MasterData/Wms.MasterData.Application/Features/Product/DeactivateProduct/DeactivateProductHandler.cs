using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Application.Features.Product.DeactivateProduct;

internal sealed class DeactivateProductHandler(IProductRepository repository)
    : ICommandHandler<DeactivateProductCommand>
{
    public async Task<Result> Handle(DeactivateProductCommand command, CancellationToken cancellationToken)
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

        return product.Deactivate();
    }
}
