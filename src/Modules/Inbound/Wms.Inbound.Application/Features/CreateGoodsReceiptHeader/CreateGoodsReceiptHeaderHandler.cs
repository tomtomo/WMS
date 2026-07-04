using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;

internal sealed class CreateGoodsReceiptHeaderHandler(
    IGoodsReceiptRepository repository,
    IWarehouseReader warehouseReader,
    IProductReader productReader) : ICommandHandler<CreateGoodsReceiptHeaderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGoodsReceiptHeaderCommand command, CancellationToken cancellationToken)
    {
        // Lookup master data
        if (!await warehouseReader.ExistsAsync(command.WarehouseId, cancellationToken))
        {
            return Result.Invalid<Guid>(new Error("goods_receipt.warehouse_unknown", "WarehouseId tidak dikenal di Master Data."));
        }

        foreach (var sku in command.ExpectedLines.Select(line => line.Sku).Distinct(StringComparer.Ordinal))
        {
            if (!await productReader.ExistsAsync(sku, cancellationToken))
            {
                return Result.Invalid<Guid>(new Error("goods_receipt.sku_unknown", $"SKU '{sku}' tidak dikenal di Master Data."));
            }
        }

        var dockDoor = DockDoor.Create(command.DockDoor);
        if (dockDoor.IsFailure)
        {
            return dockDoor.ForwardFailure<Guid>();
        }

        var expectedLines = new List<ExpectedLine>(command.ExpectedLines.Count);
        foreach (var input in command.ExpectedLines)
        {
            var line = ExpectedLine.Create(input.Sku, input.ExpectedQty, input.Uom);
            if (line.IsFailure)
            {
                return line.ForwardFailure<Guid>();
            }

            expectedLines.Add(line.Value);
        }

        // Id dibentuk di Application
        var id = GoodsReceiptId.Create(Guid.NewGuid());
        var goodsReceipt = GoodsReceipt.Create(
            id.Value,
            command.PoRef,
            command.SupplierId,
            command.WarehouseId,
            dockDoor.Value,
            expectedLines);
        if (goodsReceipt.IsFailure)
        {
            return goodsReceipt.ForwardFailure<Guid>();
        }

        await repository.AddAsync(goodsReceipt.Value, cancellationToken);
        return Result.Success(goodsReceipt.Value.Id.Value);
    }
}
