using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;
using DomainProduct = Wms.MasterData.Domain.Product;

namespace Wms.MasterData.Application.Features.Product.CreateProduct;

internal sealed class CreateProductHandler(IProductRepository repository)
    : ICommandHandler<CreateProductCommand, string>
{
    public async Task<Result<string>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sku = Sku.Create(command.Sku);
        if (sku.IsFailure)
        {
            return sku.ForwardFailure<string>();
        }

        if (await repository.ExistsAsync(sku.Value, cancellationToken))
        {
            return Result.Conflict<string>(new Error("product.already_exists", "SKU sudah terdaftar."));
        }

        var product = DomainProduct.Create(
            sku.Value,
            command.Name,
            command.Uom,
            command.BatchTrackingRequired,
            command.ExpiryTrackingRequired,
            command.QcRequiredOnReceipt,
            command.ShelfLifeDays);
        if (product.IsFailure)
        {
            return product.ForwardFailure<string>();
        }

        await repository.AddAsync(product.Value, cancellationToken);
        return Result.Success(product.Value.Sku.Value);
    }
}
