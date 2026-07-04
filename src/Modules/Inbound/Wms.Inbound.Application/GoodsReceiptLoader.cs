using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Application;

// Id kosong maka Invalid, tidak ada baris maka NotFound.
internal static class GoodsReceiptLoader
{
    public static async Task<Result<GoodsReceipt>> LoadAsync(
        IGoodsReceiptRepository repository,
        Guid goodsReceiptId,
        CancellationToken cancellationToken)
    {
        var id = GoodsReceiptId.Create(goodsReceiptId);
        if (id.IsFailure)
        {
            return id.ForwardFailure<GoodsReceipt>();
        }

        var goodsReceipt = await repository.GetAsync(id.Value, cancellationToken);
        return goodsReceipt is null
            ? Result.NotFound<GoodsReceipt>(new Error("goods_receipt.not_found", "GoodsReceipt tidak ditemukan."))
            : Result.Success(goodsReceipt);
    }
}
